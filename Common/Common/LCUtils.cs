using System.Text;

namespace LeanCloud.Common {
    public static class LCUtils {
        public static string ToHex(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++) {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
