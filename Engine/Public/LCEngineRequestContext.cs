using LeanCloud.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace LeanCloud.Engine {
    public class LCEngineRequestContext {
        private static readonly string SESSION_HEADER_KEY = "x-lc-session";
        private static readonly string REAL_IP_HEADER_KEY = "x-real-ip";
        private static readonly string FORWARD_FOR_HEADER_KEY = "x-forwarded-for";

        public string RemoteAddress { get; set; }
        public string SessionToken { get; set; }
        public LCUser CurrentUser { get; set; }

        public LCEngineRequestContext(HttpRequest request) {
            RemoteAddress = GetIP(request);
            if (request.Headers.TryGetValue(SESSION_HEADER_KEY, out StringValues session)) {
                SessionToken = session;
            }
        }

        static string GetIP(HttpRequest request) {
            if (request.Headers.TryGetValue(REAL_IP_HEADER_KEY, out StringValues ip)) {
                return ip.ToString();
            }
            if (request.Headers.TryGetValue(FORWARD_FOR_HEADER_KEY, out StringValues forward)) {
                return forward.ToString();
            }
            return request.HttpContext.Connection.RemoteIpAddress.ToString();
        }
    }
}
