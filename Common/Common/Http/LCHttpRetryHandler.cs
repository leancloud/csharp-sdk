using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Common {
    public class LCHttpRetryHandler : DelegatingHandler {
        static readonly int MAX_RETRIES = 5;

        public LCHttpRetryHandler(HttpMessageHandler innerHandler) : base(innerHandler) {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Exception ex = null;
            int retryCount = 0;
            while (retryCount < MAX_RETRIES) {
                try {
                    HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
                    return response;
                } catch (Exception e) {
                    retryCount++;
                    ex = e;
                    await Task.Delay(TimeSpan.FromSeconds(retryCount));
                }
            }

            if (ex != null) {
                throw ex;
            }

            return null;
        }
    }
}
