using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Common;

namespace LeanCloud.Storage {
    /// <summary>
    /// LeanCloud SMS Client
    /// </summary>
    public static class LCSMSClient {
        /// <summary>
        /// Requests an SMS code for operation verification.
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="template"></param>
        /// <param name="signature"></param>
        /// <param name="captchaToken"></param>
        /// <param name="variables">Template variables</param>
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
            await LCCore.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }

        /// <summary>
        /// Requests to send the verification code via phone call.
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static async Task RequestVoiceCode(string mobile) {
            string path = "requestSmsCode";
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile },
                { "smsType", "voice" }
            };
            await LCCore.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }

        /// <summary>
        /// Verifies the mobile number with the verification code.
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static async Task VerifyMobilePhone(string mobile, string code) {
            string path = $"verifySmsCode/{code}";
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile }
            };
            await LCCore.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }
    }
}
