using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Security.Cryptography;
using LeanCloud.Common;
using LC.Newtonsoft.Json;

namespace LeanCloud.Storage.Internal.File {
    class QiniuBlock {
        [JsonProperty("etag")]
        public string ETag {
            get; set;
        }

        [JsonProperty("md5")]
        public string MD5 {
            get; set;
        }
    }

    class QiniuPart {
        [JsonProperty("partNumber")]
        public int PartNumber {
            get; set;
        }

        [JsonProperty("etag")]
        public string ETag {
            get; set;
        }
    }

    internal class LCQiniuUploader {
        private string uploadUrl;

        private string token;

        private string bucket;

        private string key;

        private Stream stream;

        internal LCQiniuUploader(string uploadUrl, string token, string bucket, string key, Stream stream) {
            this.uploadUrl = uploadUrl;
            this.token = token;
            this.bucket = bucket;
            this.key = key;
            this.stream = stream;
        }

        internal async Task Upload(Action<long, long> onProgress) {
            string encodedObjectName = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
                .Replace("+", "-")
                .Replace("/", "_");

            HttpClient client = new HttpClient();

            Uri uri = new Uri(uploadUrl);
            client.DefaultRequestHeaders.Host = uri.Host;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("UpToken", token);

            // 1. 初始化任务，请求 upload id
            string uploadId = await RequestUploadId(client, encodedObjectName);

            // 2. 分块上传数据
            List<QiniuPart> parts = await UploadBlocks(client, encodedObjectName, uploadId, onProgress);

            // 3. 完成文件上传
            await FinishUpload(client, encodedObjectName, uploadId, parts);
        }

        async Task<string> RequestUploadId(HttpClient client, string encodedObjectName) {
            string endpoint = $"buckets/{bucket}/objects/{encodedObjectName}/uploads";
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri($"{uploadUrl}/{endpoint}"),
                Method = HttpMethod.Post,
            };

            LCHttpUtils.PrintRequest(client, request);

            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();
            
            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            LCHttpUtils.PrintResponse(response, resultString);

            if (!response.IsSuccessStatusCode) {
                throw new Exception(resultString);
            }

            Dictionary<string, object> res = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString,
                LCJsonConverter.Default);
            string uploadId = res["uploadId"] as string;

            return uploadId;
        }

        async Task<List<QiniuPart>> UploadBlocks(HttpClient client, string encodedObjectName, string uploadId, Action<long, long> onProgress) {
            int size = 4 * 1024 * 1024;
            byte[] buffer = new byte[size];
            long blockCount = (stream.Length + buffer.Length - 1) / buffer.Length;
            List<QiniuPart> parts = new List<QiniuPart>();
            for (int i = 1; i <= blockCount; i++) {
                int count = await stream.ReadAsync(buffer, 0, Math.Min((int)(stream.Length - (i - 1) * size), size));
                string endpoint = $"buckets/{bucket}/objects/{encodedObjectName}/uploads/{uploadId}/{i}";

                MemoryStream memoryStream = new MemoryStream(buffer, 0, count);

                LCProgressableStreamContent content = new LCProgressableStreamContent(memoryStream, (uploaded, _) => {
                    long totalUploaded = size * (i - 1) + uploaded;
                    onProgress?.Invoke(totalUploaded, stream.Length);
                });
                HttpRequestMessage request = new HttpRequestMessage {
                    RequestUri = new Uri($"{uploadUrl}/{endpoint}"),
                    Method = HttpMethod.Put,
                    Content = content
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                request.Content.Headers.ContentMD5 = CalcMD5(buffer, 0, count);
                request.Content.Headers.ContentLength = count;
                LCHttpUtils.PrintRequest(client, request);

                HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                request.Dispose();

                string resultString = await response.Content.ReadAsStringAsync();
                response.Dispose();
                LCHttpUtils.PrintResponse(response, resultString);

                if (!response.IsSuccessStatusCode) {
                    throw new Exception(resultString);
                }

                QiniuBlock block = JsonConvert.DeserializeObject<QiniuBlock>(resultString,
                    LCJsonConverter.Default);

                QiniuPart part = new QiniuPart();
                part.PartNumber = i;
                part.ETag = block.ETag;
                parts.Add(part);
            }

            return parts;
        }

        async Task FinishUpload(HttpClient client, string encodedObjectName, string uploadId, List<QiniuPart> parts) {
            string endpoint = $"buckets/{bucket}/objects/{encodedObjectName}/uploads/{uploadId}";
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "parts", parts }
            };

            string body = JsonConvert.SerializeObject(data);
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri($"{uploadUrl}/{endpoint}"),
                Method = HttpMethod.Post,
                Content = new StringContent(body)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            LCHttpUtils.PrintRequest(client, request, body);

            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            LCHttpUtils.PrintResponse(response, resultString);

            if (!response.IsSuccessStatusCode) {
                throw new Exception(resultString);
            }
        }

        static byte[] CalcMD5(byte[] buffer, int index, int count) {
            MD5 md5 = MD5.Create();
            byte[] data = md5.ComputeHash(buffer, index, count);
            return data;
        }
    }
}
