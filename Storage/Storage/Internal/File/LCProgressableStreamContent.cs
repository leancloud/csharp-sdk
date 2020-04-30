using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal.File {
    internal class LCProgressableStreamContent : HttpContent {
        const int defaultBufferSize = 5 * 4096;

        readonly HttpContent content;

        readonly int bufferSize;

        readonly Action<long, long> progress;

        internal LCProgressableStreamContent(HttpContent content, Action<long, long> progress) : this(content, defaultBufferSize, progress) { }

        internal LCProgressableStreamContent(HttpContent content, int bufferSize, Action<long, long> progress) {
            if (content == null) {
                throw new ArgumentNullException("content");
            }
            if (bufferSize <= 0) {
                throw new ArgumentOutOfRangeException("bufferSize");
            }

            this.content = content;
            this.bufferSize = bufferSize;
            this.progress = progress;

            foreach (var h in content.Headers) {
                Headers.Add(h.Key, h.Value);
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) {

            return Task.Run(async () => {
                var buffer = new byte[bufferSize];
                TryComputeLength(out long size);
                var uploaded = 0;

                using (var sinput = await content.ReadAsStreamAsync()) {
                    while (true) {
                        var length = sinput.Read(buffer, 0, buffer.Length);
                        if (length <= 0) break;

                        uploaded += length;
                        progress?.Invoke(uploaded, size);

                        stream.Write(buffer, 0, length);
                        stream.Flush();
                    }
                }
                stream.Flush();
            });
        }

        protected override bool TryComputeLength(out long length) {
            length = content.Headers.ContentLength.GetValueOrDefault();
            return true;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                content.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
