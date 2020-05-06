using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeanCloud.Storage {
    /// <summary>
    /// 短信工具类
    /// </summary>
    public static class LCSMSClient {
        /// <summary>
        /// 请求短信验证码
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="template"></param>
        /// <param name="signature"></param>
        /// <param name="captchaToken"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        public static async Task RequestSMSCode(string mobile,
            string template = null,
            string signature = null,
            string captchaToken = null,
            Dictionary<string, object> variables = null) {
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(mobile));
            }

            string path = "requestSmsCode";
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile }
            };
            if (!string.IsNullOrEmpty(template)) {
                data["template"] = template;
            }
            if (!string.IsNullOrEmpty(signature)) {
                data["sign"] = signature;
            }
            if (!string.IsNullOrEmpty(captchaToken)) {
                data["validate_token"] = captchaToken;
            }
            if (variables != null) {
                foreach (KeyValuePair<string, object> kv in variables) {
                    data[kv.Key] = kv.Value;
                }
            }
            await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }

        /// <summary>
        /// 请求语音验证码
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static async Task RequestVoiceCode(string mobile) {
            string path = "requestSmsCode";
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile },
                { "smsType", "voice" }
            };
            await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }

        /// <summary>
        /// 验证手机号
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static async Task VerifyMobilePhone(string mobile, string code) {
            string path = $"verifySmsCode/{code}";
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile }
            };
            await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }
    }
}
