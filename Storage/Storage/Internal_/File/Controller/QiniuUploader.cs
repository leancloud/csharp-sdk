using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using LeanCloud.Common;

namespace LeanCloud.Storage.Internal {
    internal enum CommonSize : long {
        MB4 = 1024 * 1024 * 4,
        MB1 = 1024 * 1024,
        KB512 = 1024 * 1024 / 2,
        KB256 = 1024 * 1024 / 4
    }

    internal class QiniuUploader {
        private static readonly int BLOCKSIZE = 1024 * 1024 * 4;
        internal static string UP_HOST = "https://up.qbox.me";
        private readonly object mutex = new object();

        public int counter;
        public Stream frozenData;
        public string bucketId;
        public string bucket;
        public string token;
        public long completed;
        public List<string> block_ctxes = new List<string>();

        internal string Key {
            get; set;
        }

        internal string Token {
            get; set;
        }

        internal string MimeType {
            get; set;
        }

        internal IDictionary<string, object> MetaData {
            get; set;
        }

        internal Stream Stream {
            get; set;
        }

        internal async Task Upload(CancellationToken cancellationToken = default) {
            await UploadNextChunk(string.Empty, 0, null);
        }

        async Task UploadNextChunk(string context, long offset, IProgress<AVUploadProgressEventArgs> progress) {
            var totalSize = Stream.Length;
            var remainingSize = totalSize - completed;

            if (progress != null) {
                lock (mutex) {
                    progress.Report(new AVUploadProgressEventArgs() {
                        Progress = AVFileController.CalcProgress(completed, totalSize)
                    });
                }
            }
            if (completed == totalSize) {
                await QiniuMakeFile(totalSize, block_ctxes.ToArray(), CancellationToken.None);
            } else if (completed % BLOCKSIZE == 0) {
                var firstChunkBinary = GetChunkBinary();

                var blockSize = remainingSize > BLOCKSIZE ? BLOCKSIZE : remainingSize;
                var result = await MakeBlock(firstChunkBinary, blockSize);
                var dict = result;
                var ctx = dict["ctx"].ToString();
                offset = long.Parse(dict["offset"].ToString());
                var host = dict["host"].ToString();

                completed += firstChunkBinary.Length;
                if (completed % BLOCKSIZE == 0 || completed == totalSize) {
                    block_ctxes.Add(ctx);
                }

                await UploadNextChunk(ctx, offset, progress);
            } else {
                var chunkBinary = GetChunkBinary();
                var result = await PutChunk(chunkBinary, context, offset);
                var dict = result;
                var ctx = dict["ctx"].ToString();

                offset = long.Parse(dict["offset"].ToString());
                var host = dict["host"].ToString();
                completed += chunkBinary.Length;
                if (completed % BLOCKSIZE == 0 || completed == totalSize) {
                    block_ctxes.Add(ctx);
                }
                await UploadNextChunk(ctx, offset, progress);
            }   
        }

        byte[] GetChunkBinary() {
            long chunkSize = (long)CommonSize.MB1;
            if (completed + chunkSize > Stream.Length) {
                chunkSize = Stream.Length - completed;
            }
            byte[] chunkBinary = new byte[chunkSize];
            Stream.Seek(completed, SeekOrigin.Begin);
            Stream.Read(chunkBinary, 0, (int)chunkSize);
            return chunkBinary;
        }

        IList<KeyValuePair<string, string>> GetQiniuRequestHeaders() {
            IList<KeyValuePair<string, string>> makeBlockHeaders = new List<KeyValuePair<string, string>>();
            string authHead = "UpToken " + Token;
            makeBlockHeaders.Add(new KeyValuePair<string, string>("Authorization", authHead));
            return makeBlockHeaders;
        }

        async Task<Dictionary<string, object>> MakeBlock(byte[] firstChunkBinary, long blcokSize = 4194304) {
            MemoryStream firstChunkData = new MemoryStream(firstChunkBinary, 0, firstChunkBinary.Length);

            var client = new HttpClient();
            var request = new HttpRequestMessage {
                RequestUri = new Uri($"{UP_HOST}/mkblk/{blcokSize}"),
                Method = HttpMethod.Post,
                Content = new StreamContent(firstChunkData)
            };
            var headers = GetQiniuRequestHeaders();
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

        async Task<Dictionary<string, object>> PutChunk(byte[] chunkBinary, string LastChunkctx, long currentChunkOffsetInBlock) {
            MemoryStream chunkData = new MemoryStream(chunkBinary, 0, chunkBinary.Length);
            var client = new HttpClient();
            var request = new HttpRequestMessage {
                RequestUri = new Uri($"{UP_HOST}/bput/{LastChunkctx}/{currentChunkOffsetInBlock}"),
                Method = HttpMethod.Post,
                Content = new StreamContent(chunkData)
            };
            var headers = GetQiniuRequestHeaders();
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

        internal async Task<Tuple<HttpStatusCode, string>> QiniuMakeFile(long fsize, string[] ctxes, CancellationToken cancellationToken) {
            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.AppendFormat("{0}/mkfile/{1}", UP_HOST, fsize);
            if (!string.IsNullOrEmpty(Key)) {
                urlBuilder.AppendFormat("/key/{0}", ToBase64URLSafe(Key));
            }
            var metaData = GetMetaData();

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
            request.Headers.Add("Authorization", $"UpToken {Token}");
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

        internal IDictionary<string, object> GetMetaData() {
            IDictionary<string, object> rtn = new Dictionary<string, object>();

            if (MetaData != null) {
                foreach (var meta in MetaData) {
                    rtn.Add(meta.Key, meta.Value);
                }
            }
            MergeDic(rtn, "mime_type", AVFile.GetMIMEType(MimeType));
            MergeDic(rtn, "size", Stream.Length);
            MergeDic(rtn, "_checksum", GetMD5Code(Stream));
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
