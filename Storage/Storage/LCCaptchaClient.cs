using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeanCloud.Storage {
    /// <summary>
    /// An image CAPTCHA to prevent SMS abuse.
    /// </summary>
    public class LCCapture {
        public string Url {
            get; set;
        }

        public string Token {
            get; set;
        }
    }

    /// <summary>
    /// Requests a CAPTCHA image and sends the verification code.
    /// </summary>
    public static class LCCaptchaClient {
        /// <summary>
        /// Requests a CAPTCHA image from LeanCloud.
        /// </summary>
        /// <param name="width">Width of the CAPTCHA image.</param>
        /// <param name="height">Height of the CAPTCHA image.</param>
        /// <returns></returns>
        public static async Task<LCCapture> RequestCaptcha(int width = 82,
            int height = 39) {
            string path = "requestCaptcha";
            Dictionary<string, object> queryParams = new Dictionary<string, object> {
                { "width", width },
                { "height", height }
            };
            Dictionary<string, object> response = await LCInternalApplication.HttpClient.Get<Dictionary<string, object>>(path, queryParams: queryParams);
            return new LCCapture {
                Url = response["captcha_url"] as string,
                Token = response["captcha_token"] as string
            };
        }

        /// <summary>
        /// Sends the code to LeanCloud for verification.
        /// </summary>
        /// <param name="code">entered by the user</param>
        /// <param name="token">for LeanCloud to recognize which CAPTCHA to verify</param>
        /// <returns></returns>
        public static async Task VerifyCaptcha(string code,
            string token) {
            if (string.IsNullOrEmpty(code)) {
                throw new ArgumentNullException(nameof(code));
            }
            if (string.IsNullOrEmpty(token)) {
                throw new ArgumentNullException(nameof(token));
            }

            string path = "verifyCaptcha";
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "captcha_code", code },
                { "captcha_token", token }
            };
            await LCInternalApplication.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }
    }
}
