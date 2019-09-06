using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace LeanCloud.Storage.Internal {
    internal enum CommonSize : long {
        MB4 = 1024 * 1024 * 4,
        MB1 = 1024 * 1024,
        KB512 = 1024 * 1024 / 2,
        KB256 = 1024 * 1024 / 4
    }

    internal class QiniuUploader : IFileUploader {
        private static readonly int BLOCKSIZE = 1024 * 1024 * 4;
        internal static string UP_HOST = "https://up.qbox.me";
        private readonly object mutex = new object();

        public async Task<FileState> Upload(FileState state, Stream dataStream, IDictionary<string, object> fileToken, IProgress<AVUploadProgressEventArgs> progress, CancellationToken cancellationToken) {
            state.frozenData = dataStream;
            state.CloudName = fileToken["key"] as string;
            MergeFromJSON(state, fileToken);
            await UploadNextChunk(state, dataStream, string.Empty, 0, progress);
            return state;
        }

        async Task UploadNextChunk(FileState state, Stream dataStream, string context, long offset, IProgress<AVUploadProgressEventArgs> progress) {
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
                await QiniuMakeFile(state, state.frozenData, state.token, state.CloudName, totalSize, state.block_ctxes.ToArray(), CancellationToken.None);
            } else if (state.completed % BLOCKSIZE == 0) {
                var firstChunkBinary = GetChunkBinary(state.completed, dataStream);

                var blockSize = remainingSize > BLOCKSIZE ? BLOCKSIZE : remainingSize;
                var result = await MakeBlock(state, firstChunkBinary, blockSize);
                var dict = result;
                var ctx = dict["ctx"].ToString();
                offset = long.Parse(dict["offset"].ToString());
                var host = dict["host"].ToString();

                state.completed += firstChunkBinary.Length;
                if (state.completed % BLOCKSIZE == 0 || state.completed == totalSize) {
                    state.block_ctxes.Add(ctx);
                }

                await UploadNextChunk(state, dataStream, ctx, offset, progress);
            } else {
                var chunkBinary = GetChunkBinary(state.completed, dataStream);
                var result = await PutChunk(state, chunkBinary, context, offset);
                var dict = result;
                var ctx = dict["ctx"].ToString();

                offset = long.Parse(dict["offset"].ToString());
                var host = dict["host"].ToString();
                state.completed += chunkBinary.Length;
                if (state.completed % BLOCKSIZE == 0 || state.completed == totalSize) {
                    state.block_ctxes.Add(ctx);
                }
                await UploadNextChunk(state, dataStream, ctx, offset, progress);
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

        IList<KeyValuePair<string, string>> GetQiniuRequestHeaders(FileState state) {
            IList<KeyValuePair<string, string>> makeBlockHeaders = new List<KeyValuePair<string, string>>();
            string authHead = "UpToken " + state.token;
            makeBlockHeaders.Add(new KeyValuePair<string, string>("Authorization", authHead));
            return makeBlockHeaders;
        }

        async Task<Dictionary<string, object>> MakeBlock(FileState state, byte[] firstChunkBinary, long blcokSize = 4194304) {
            MemoryStream firstChunkData = new MemoryStream(firstChunkBinary, 0, firstChunkBinary.Length);

            var client = new HttpClient();
            var request = new HttpRequestMessage {
                RequestUri = new Uri($"{UP_HOST}/mkblk/{blcokSize}"),
                Method = HttpMethod.Post,
                Content = new StreamContent(firstChunkData)
            };
            var headers = GetQiniuRequestHeaders(state);
            foreach (var header in headers) {
                request.Headers.Add(header.Key, header.Value);
            }

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var response = await client.SendAsync(request);
            client.Dispose();
            request.Dispose();
            var content = await response.Content.ReadAsStringAsync();
            response.Dispose();
            return await JsonUtils.DeserializeObjectAsync<Dictionary<string, object>>(content, new LeanCloudJsonConverter());
        }

        async Task<Dictionary<string, object>> PutChunk(FileState state, byte[] chunkBinary, string LastChunkctx, long currentChunkOffsetInBlock) {
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
            return await JsonUtils.DeserializeObjectAsync<Dictionary<string, object>>(content);
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
            request.Headers.Add("Authorization", $"UpToken {upToken}");
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

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
            if (string.IsNullOrEmpty(text))
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
