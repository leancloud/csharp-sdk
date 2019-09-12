using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LeanCloud.Storage.Internal {
    internal class AWSUploader {
        internal string UploadUrl {
            get; set;
        }

        internal string MimeType {
            get; set;
        }

        internal Stream Stream {
            get; set;
        }

        internal async Task Upload(CancellationToken cancellationToken = default) {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(UploadUrl),
                Method = HttpMethod.Put,
                Content = new StreamContent(Stream)
            };
            request.Headers.Add("Cache-Control", "public, max-age=31536000");
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(MimeType);
            request.Content.Headers.ContentLength = Stream.Length;
            HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            response.Dispose();
            client.Dispose();
            request.Dispose();
        }
    }
}
