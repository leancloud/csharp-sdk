using System.Linq;
using System.Text;
using System.Net.Http;

namespace LeanCloud.Common {
    public static class LCHttpUtils {
        public static void PrintRequest(HttpClient client, HttpRequestMessage request, string content = null) {
            if (client == null) {
                return;
            }
            if (request == null) {
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== HTTP Request Start ===");
            sb.AppendLine($"URL: {request.RequestUri}");
            sb.AppendLine($"Method: {request.Method}");
            sb.AppendLine($"Headers: ");
            foreach (var header in client.DefaultRequestHeaders) {
                sb.AppendLine($"\t{header.Key}: {string.Join(",", header.Value.ToArray())}");
            }
            foreach (var header in request.Headers) {
                sb.AppendLine($"\t{header.Key}: {string.Join(",", header.Value.ToArray())}");
            }
            if (request.Content != null) {
                foreach (var header in request.Content.Headers) {
                    sb.AppendLine($"\t{header.Key}: {string.Join(",", header.Value.ToArray())}");
                }
            }
            if (!string.IsNullOrEmpty(content)) {
                sb.AppendLine($"Content: {content}");
            }
            sb.AppendLine("=== HTTP Request End ===");
            LCLogger.Debug(sb.ToString());
        }

        public static void PrintResponse(HttpResponseMessage response, string content = null) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== HTTP Response Start ===");
            sb.AppendLine($"URL: {response.RequestMessage.RequestUri}");
            sb.AppendLine($"Status Code: {response.StatusCode}");
            if (!string.IsNullOrEmpty(content)) {
                sb.AppendLine($"Content: {content}");
            }
            sb.AppendLine("=== HTTP Response End ===");
            LCLogger.Debug(sb.ToString());
        }
    }
}
