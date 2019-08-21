using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace LeanCloud.Storage.Internal {
    internal enum CommonSize : long {
        MB4 = 1024 * 1024 * 4,
        MB1 = 1024 * 1024,
        KB512 = 1024 * 1024 / 2,
        KB256 = 1024 * 1024 / 4
    }

    internal class QiniuUploader : IFileUploader {
        private static int BLOCKSIZE = 1024 * 1024 * 4;
        private const int blockMashk = (1 << blockBits) - 1;
        private const int blockBits = 22;
        private int CalcBlockCount(long fsize) {
            return (int)((fsize + blockMashk) >> blockBits);
        }
        internal static string UP_HOST = "https://up.qbox.me";
        private object mutex = new object();

        public Task<FileState> Upload(FileState state, Stream dataStream, IDictionary<string, object> fileToken, IProgress<AVUploadProgressEventArgs> progress, CancellationToken cancellationToken) {
            state.frozenData = dataStream;
            state.CloudName = GetUniqueName(state);
            MergeFromJSON(state, fileToken);
            return UploadNextChunk(state, dataStream, string.Empty, 0, progress).OnSuccess(_ => {
                return state;
            });
        }

        Task UploadNextChunk(FileState state, Stream dataStream, string context, long offset, IProgress<AVUploadProgressEventArgs> progress) {
            var totalSize = dataStream.Length;
            var remainingSize = totalSize - state.completed;

            if (progress != null) {
                lock (mutex) {
                    progress.Report(new AVUploadProgressEventArgs() {
                        Progress = AVFileController.CalcProgress(state.completed, totalSize)
                    });
                }
            }
            if (state.completed == totalSize) {
                return QiniuMakeFile(state, state.frozenData, state.token, state.CloudName, totalSize, state.block_ctxes.ToArray(), CancellationToken.None);

            } else if (state.completed % BLOCKSIZE == 0) {
                var firstChunkBinary = GetChunkBinary(state.completed, dataStream);

                var blockSize = remainingSize > BLOCKSIZE ? BLOCKSIZE : remainingSize;
                return MakeBlock(state, firstChunkBinary, blockSize).ContinueWith(t => {
                    var dict = JsonConvert.DeserializeObject<IDictionary<string, object>>(t.Result.Item2, new LeanCloudJsonConverter());
                    var ctx = dict["ctx"].ToString();
                    offset = long.Parse(dict["offset"].ToString());
                    var host = dict["host"].ToString();

                    state.completed += firstChunkBinary.Length;
                    if (state.completed % BLOCKSIZE == 0 || state.completed == totalSize) {
                        state.block_ctxes.Add(ctx);
                    }

                    return UploadNextChunk(state, dataStream, ctx, offset, progress);
                }).Unwrap();

            } else {
                var chunkBinary = GetChunkBinary(state.completed, dataStream);
                return PutChunk(state, chunkBinary, context, offset).ContinueWith(t => {
                    var dict = JsonConvert.DeserializeObject<IDictionary<string, object>>(t.Result.Item2, new LeanCloudJsonConverter());
                    var ctx = dict["ctx"].ToString();

                    offset = long.Parse(dict["offset"].ToString());
                    var host = dict["host"].ToString();
                    state.completed += chunkBinary.Length;
                    if (state.completed % BLOCKSIZE == 0 || state.completed == totalSize) {
                        state.block_ctxes.Add(ctx);
                    }
                    //if (AVClient.fileUploaderDebugLog)
                    //{
                    //    AVClient.LogTracker(state.counter + "|completed=" + state.completed + "stream:position=" + dataStream.Position + "|");
                    //}

                    return UploadNextChunk(state, dataStream, ctx, offset, progress);
                }).Unwrap();
            }
        }

        byte[] GetChunkBinary(long completed, Stream dataStream) {
            long chunkSize = (long)CommonSize.MB1;
            if (completed + chunkSize > dataStream.Length) {
                chunkSize = dataStream.Length - completed;
            }
            byte[] chunkBinary = new byte[chunkSize];
            dataStream.Seek(completed, SeekOrigin.Begin);
            dataStream.Read(chunkBinary, 0, (int)chunkSize);
            return chunkBinary;
        }

        internal string GetUniqueName(FileState state) {
            string key = Guid.NewGuid().ToString();//file Key in Qiniu.
            string extension = Path.GetExtension(state.Name);
            key += extension;
            return key;
        }
        internal Task<Tuple<HttpStatusCode, IDictionary<string, object>>> GetQiniuToken(FileState state, CancellationToken cancellationToken) {
            string str = state.Name;

            IDictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("name", str);
            parameters.Add("key", state.CloudName);
            parameters.Add("__type", "File");
            parameters.Add("mime_type", AVFile.GetMIMEType(str));

            state.MetaData = GetMetaData(state, state.frozenData);

            parameters.Add("metaData", state.MetaData);

            var command = new AVCommand {
                Path = "qiniu",
                Method = HttpMethod.Post,
                Content = parameters
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }
        IList<KeyValuePair<string, string>> GetQiniuRequestHeaders(FileState state) {
            IList<KeyValuePair<string, string>> makeBlockHeaders = new List<KeyValuePair<string, string>>();

            string authHead = "UpToken " + state.token;
            makeBlockHeaders.Add(new KeyValuePair<string, string>("Authorization", authHead));
            return makeBlockHeaders;
        }

        async Task<Tuple<HttpStatusCode, string>> MakeBlock(FileState state, byte[] firstChunkBinary, long blcokSize = 4194304) {
            MemoryStream firstChunkData = new MemoryStream(firstChunkBinary, 0, firstChunkBinary.Length);
            var headers = GetQiniuRequestHeaders(state);
            headers.Add(new KeyValuePair<string, string>("Content-Type", "application/octet-stream"));
            var client = new HttpClient();
            var request = new HttpRequestMessage {
                RequestUri = new Uri($"{UP_HOST}/mkblk/{blcokSize}"),
                Method = HttpMethod.Post,
                Content = new StreamContent(firstChunkData)
            };
            foreach (var header in headers) {
                request.Headers.Add(header.Key, header.Value);
            }
            var response = await client.SendAsync(request);
            client.Dispose();
            request.Dispose();
            var content = await response.Content.ReadAsStringAsync();
            response.Dispose();
            return await JsonUtils.DeserializeObjectAsync<Tuple<HttpStatusCode, string>>(content);
        }

        async Task<Tuple<HttpStatusCode, string>> PutChunk(FileState state, byte[] chunkBinary, string LastChunkctx, long currentChunkOffsetInBlock) {
            MemoryStream chunkData = new MemoryStream(chunkBinary, 0, chunkBinary.Length);
            var client = new HttpClient();
            var request = new HttpRequestMessage {
                RequestUri = new Uri($"{UP_HOST}/bput/{LastChunkctx}/{currentChunkOffsetInBlock}"),
                Method = HttpMethod.Post,
                Content = new StreamContent(chunkData)
            };
            var headers = GetQiniuRequestHeaders(state);
            foreach (var header in headers) {
                request.Headers.Add(header.Key, header.Value);
            }
            var response = await client.SendAsync(request);
            client.Dispose();
            request.Dispose();
            var content = await response.Content.ReadAsStringAsync();
            response.Dispose();
            return await JsonUtils.DeserializeObjectAsync<Tuple<HttpStatusCode, string>>(content);
        }

        internal async Task<Tuple<HttpStatusCode, string>> QiniuMakeFile(FileState state, Stream dataStream, string upToken, string key, long fsize, string[] ctxes, CancellationToken cancellationToken) {
            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.AppendFormat("{0}/mkfile/{1}", UP_HOST, fsize);
            if (key != null) {
                urlBuilder.AppendFormat("/key/{0}", ToBase64URLSafe(key));
            }
            var metaData = GetMetaData(state, dataStream);

            StringBuilder sb = new StringBuilder();
            foreach (string _key in metaData.Keys) {
                sb.AppendFormat("/{0}/{1}", _key, ToBase64URLSafe(metaData[_key].ToString()));
            }
            urlBuilder.Append(sb.ToString());

            IList<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
            //makeBlockDic.Add("Content-Type", "application/octet-stream");

            string authHead = "UpToken " + upToken;
            headers.Add(new KeyValuePair<string, string>("Authorization", authHead));
            headers.Add(new KeyValuePair<string, string>("Content-Type", "text/plain"));
            int proCount = ctxes.Length;
            Stream body = new MemoryStream();

            for (int i = 0; i < proCount; i++) {
                byte[] bctx = StringToAscii(ctxes[i]);
                body.Write(bctx, 0, bctx.Length);
                if (i != proCount - 1) {
                    body.WriteByte((byte)',');
                }
            }
            body.Seek(0, SeekOrigin.Begin);

            var client = new HttpClient();
            var request = new HttpRequestMessage {
                RequestUri = new Uri(urlBuilder.ToString()),
                Method = HttpMethod.Post,
                Content = new StreamContent(body)
            };
            var response = await client.SendAsync(request);
            client.Dispose();
            request.Dispose();
            var content = await response.Content.ReadAsStringAsync();
            response.Dispose();
            return await JsonUtils.DeserializeObjectAsync<Tuple<HttpStatusCode, string>>(content);
        }

        internal void MergeFromJSON(FileState state, IDictionary<string, object> jsonData) {
            lock (this.mutex) {
                string url = jsonData["url"] as string;
                state.Url = new Uri(url, UriKind.Absolute);
                state.bucketId = FetchBucketId(url);
                state.token = jsonData["token"] as string;
                state.bucket = jsonData["bucket"] as string;
                state.ObjectId = jsonData["objectId"] as string;
            }
        }

        string FetchBucketId(string url) {
            var elements = url.Split('/');

            return elements[elements.Length - 1];
        }
        public static byte[] StringToAscii(string s) {
            byte[] retval = new byte[s.Length];
            for (int ix = 0; ix < s.Length; ++ix) {
                char ch = s[ix];
                if (ch <= 0x7f)
                    retval[ix] = (byte)ch;
                else
                    retval[ix] = (byte)'?';
            }
            return retval;
        }
        public static string ToBase64URLSafe(string str) {
            return Encode(str);
        }
        public static string Encode(byte[] bs) {
            if (bs == null || bs.Length == 0)
                return "";
            string encodedStr = Convert.ToBase64String(bs);
            encodedStr = encodedStr.Replace('+', '-').Replace('/', '_');
            return encodedStr;
        }
        public static string Encode(string text) {
            if (String.IsNullOrEmpty(text))
                return "";
            byte[] bs = Encoding.UTF8.GetBytes(text);
            string encodedStr = Convert.ToBase64String(bs);
            encodedStr = encodedStr.Replace('+', '-').Replace('/', '_');
            return encodedStr;
        }

        internal static string GetMD5Code(Stream data) {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(data);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++) {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();

        }

        internal IDictionary<string, object> GetMetaData(FileState state, Stream data) {
            IDictionary<string, object> rtn = new Dictionary<string, object>();

            if (state.MetaData != null) {
                foreach (var meta in state.MetaData) {
                    rtn.Add(meta.Key, meta.Value);
                }
            }
            MergeDic(rtn, "mime_type", AVFile.GetMIMEType(state.Name));
            MergeDic(rtn, "size", data.Length);
            MergeDic(rtn, "_checksum", GetMD5Code(data));
            if (AVUser.CurrentUser != null)
                if (AVUser.CurrentUser.ObjectId != null)
                    MergeDic(rtn, "owner", AVUser.CurrentUser.ObjectId);

            return rtn;
        }
        internal void MergeDic(IDictionary<string, object> dic, string key, object value) {
            if (dic.ContainsKey(key)) {
                dic[key] = value;
            } else {
                dic.Add(key, value);
            }
        }
    }
}
