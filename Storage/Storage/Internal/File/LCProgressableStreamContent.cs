using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal.File {
    internal class LCProgressableStreamContent : HttpContent {
        const int DEFAULT_BUFFER_SIZE = 100 * 1024;

        readonly Stream content;

        readonly Action<long, long> progress;

        internal LCProgressableStreamContent(Stream content, Action<long, long> progress) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }

            this.content = content;
            this.progress = progress;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context) {
            byte[] buffer = new byte[DEFAULT_BUFFER_SIZE];
            while (true) {
                int length = await content.ReadAsync(buffer, 0, buffer.Length);
                if (length <= 0) {
                    break;
                }

                await stream.WriteAsync(buffer, 0, length);
                progress?.Invoke(content.Position, content.Length);
            }
            await stream.FlushAsync();
        }

        protected override bool TryComputeLength(out long length) {
            length = content.Length;
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
