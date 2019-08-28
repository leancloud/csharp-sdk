using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;

namespace LeanCloud {
    /// <summary>
    /// The AVCloud class provides methods for interacting with LeanCloud Cloud Functions.
    /// </summary>
    /// <example>
    /// For example, this sample code calls the
    /// "validateGame" Cloud Function and calls processResponse if the call succeeded
    /// and handleError if it failed.
    ///
    /// <code>
    /// var result =
    ///     await AVCloud.CallFunctionAsync&lt;IDictionary&lt;string, object&gt;&gt;("validateGame", parameters);
    /// </code>
    /// </example>
    public static class AVCloud
    {
        internal static IAVCloudCodeController CloudCodeController
        {
            get
            {
                return AVPlugins.Instance.CloudCodeController;
            }
        }

        /// <summary>
        /// Calls a cloud function.
        /// </summary>
        /// <typeparam name="T">The type of data you will receive from the cloud function. This
        /// can be an IDictionary, string, IList, AVObject, or any other type supported by
        /// AVObject.</typeparam>
        /// <param name="name">The cloud function to call.</param>
        /// <param name="parameters">The parameters to send to the cloud function. This
        /// dictionary can contain anything that could be passed into a AVObject except for
        /// AVObjects themselves.</param>
        /// <param name="sesstionToken"></param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the cloud call.</returns>
        public static Task<T> CallFunctionAsync<T>(string name, IDictionary<string, object> parameters = null, string sesstionToken = null, CancellationToken cancellationToken = default)
        {
            var sessionTokenTask = AVUser.TakeSessionToken(sesstionToken);

            return sessionTokenTask.OnSuccess(s =>
            {
                return CloudCodeController.CallFunctionAsync<T>(name,
                    parameters, s.Result,
                    cancellationToken);

            }).Unwrap();
        }

        /// <summary>
        /// 远程调用云函数，返回结果会反序列化为 <see cref="AVObject"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <param name="sesstionToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<T> RPCFunctionAsync<T>(string name, IDictionary<string, object> parameters = null, string sesstionToken = null, CancellationToken cancellationToken = default)
        {
            var sessionTokenTask = AVUser.TakeSessionToken(sesstionToken);

            return sessionTokenTask.OnSuccess(s =>
            {
                return CloudCodeController.RPCFunction<T>(name,
                    parameters,
                    s.Result,
                    cancellationToken);
            }).Unwrap();
        }

        /// <summary>
        /// 获取 LeanCloud 服务器的时间
        /// <remarks>
        /// 如果获取失败，将返回 DateTime.MinValue
        /// </remarks>
        /// </summary>
        /// <returns>服务器的时间</returns>
        public static Task<DateTime> GetServerDateTimeAsync()
        {
            var command = new AVCommand(relativeUri: "date",
                method: "GET",
                sessionToken: null,
                data: null);
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                DateTime rtn = DateTime.MinValue;
                if (AVClient.IsSuccessStatusCode(t.Result.Item1))
                {
                    var date = AVDecoder.Instance.Decode(t.Result.Item2);
                    if (date != null)
                    {
                        if (date is DateTime)
                        {
                            rtn = (DateTime)date;
                        }
                    }
                }
                return rtn;
            });
        }

        /// <summary>
        /// 请求发送验证码。
        /// </summary>
        /// <returns>是否发送成功。</returns>
        /// <param name="mobilePhoneNumber">手机号。</param>
        /// <param name="name">应用名称。</param>
        /// <param name="op">进行的操作名称。</param>
        /// <param name="ttl">验证码失效时间。</param>
        /// <param name="cancellationToken">Cancellation token。</param>
        public static Task<bool> RequestSMSCodeAsync(string mobilePhoneNumber, string name, string op, int ttl = 10, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(mobilePhoneNumber))
            {
                throw new AVException(AVException.ErrorCode.MobilePhoneInvalid, "Moblie Phone number is invalid.", null);
            }

            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "mobilePhoneNumber", mobilePhoneNumber },
            };
            if (!string.IsNullOrEmpty(name))
            {
                strs.Add("name", name);
            }
            if (!string.IsNullOrEmpty(op))
            {
                strs.Add("op", op);
            }
            if (ttl > 0)
            {
                strs.Add("TTL", ttl);
            }
            var command = new AVCommand("requestSmsCode",
               method: "POST",
               sessionToken: null,
               data: strs);
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).ContinueWith(t =>
            {
                return AVClient.IsSuccessStatusCode(t.Result.Item1);
            });
        }

        /// <summary>
        /// 请求发送验证码。
        /// </summary>
        /// <returns>是否发送成功。</returns>
        /// <param name="mobilePhoneNumber">手机号。</param>
        public static Task<bool> RequestSMSCodeAsync(string mobilePhoneNumber)
        {
            return AVCloud.RequestSMSCodeAsync(mobilePhoneNumber, CancellationToken.None);
        }


        /// <summary>
        /// 请求发送验证码。
        /// </summary>
        /// <returns>是否发送成功。</returns>
        /// <param name="mobilePhoneNumber">手机号。</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public static Task<bool> RequestSMSCodeAsync(string mobilePhoneNumber, CancellationToken cancellationToken)
        {
            return AVCloud.RequestSMSCodeAsync(mobilePhoneNumber, null, null, 0, cancellationToken);
        }

        /// <summary>
        /// 发送手机短信，并指定模板以及传入模板所需的参数。
        /// Exceptions:
        ///   AVOSCloud.AVException:
        ///   手机号为空。
        /// <param name="mobilePhoneNumber"></param>
        /// <param name="template">Sms's template</param>
        /// <param name="env">Template variables env.</param>
        /// <param name="sign">Sms's sign.</param>
        /// <returns></returns>
        public static Task<bool> RequestSMSCodeAsync(
            string mobilePhoneNumber,
            string template,
            IDictionary<string, object> env,
            string sign = "",
            string validateToken = "")
        {

            if (string.IsNullOrEmpty(mobilePhoneNumber))
            {
                throw new AVException(AVException.ErrorCode.MobilePhoneInvalid, "Moblie Phone number is invalid.", null);
            }
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "mobilePhoneNumber", mobilePhoneNumber },
            };
            strs.Add("template", template);
            if (string.IsNullOrEmpty(sign))
            {
                strs.Add("sign", sign);
            }
            if (string.IsNullOrEmpty(validateToken))
            {
                strs.Add("validate_token", validateToken);
            }
            foreach (var key in env.Keys)
            {
                strs.Add(key, env[key]);
            }
            var command = new AVCommand("requestSmsCode",
                method: "POST",
                sessionToken: null,
                data: strs);
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                return AVClient.IsSuccessStatusCode(t.Result.Item1);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mobilePhoneNumber"></param>
        /// <returns></returns>
        public static Task<bool> RequestVoiceCodeAsync(string mobilePhoneNumber)
        {
            if (string.IsNullOrEmpty(mobilePhoneNumber))
            {
                throw new AVException(AVException.ErrorCode.MobilePhoneInvalid, "Moblie Phone number is invalid.", null);
            }
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "mobilePhoneNumber", mobilePhoneNumber },
                { "smsType", "voice" },
                { "IDD","+86" }
            };

            var command = new AVCommand("requestSmsCode",
                method: "POST",
                sessionToken: null,
                data: strs);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                return AVClient.IsSuccessStatusCode(t.Result.Item1);
            });
        }

        /// <summary>
        /// 验证是否是有效短信验证码。
        /// </summary>
        /// <returns>是否验证通过。</returns>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="code">验证码。</param>
        public static Task<bool> VerifySmsCodeAsync(string code, string mobilePhoneNumber)
        {
            return AVCloud.VerifySmsCodeAsync(code, mobilePhoneNumber, CancellationToken.None);
        }

        /// <summary>
        /// 验证是否是有效短信验证码。
        /// </summary>
        /// <returns>是否验证通过。</returns>
        /// <param name="code">验证码。</param>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Task<bool> VerifySmsCodeAsync(string code, string mobilePhoneNumber, CancellationToken cancellationToken)
        {
            var command = new AVCommand("verifySmsCode/" + code.Trim() + "?mobilePhoneNumber=" + mobilePhoneNumber.Trim(),
                method: "POST",
                sessionToken: null,
                data: null);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).ContinueWith(t =>
            {
                return AVClient.IsSuccessStatusCode(t.Result.Item1);
            });
        }

        /// <summary>
        /// Stands for a captcha result.
        /// </summary>
        public class Captcha
        {
            /// <summary>
            /// Used for captcha verify.
            /// </summary>
            public string Token { internal set; get; }

            /// <summary>
            /// Captcha image URL.
            /// </summary>
            public string Url { internal set; get; }

            /// <summary>
            /// Verify the user's input of catpcha.
            /// </summary>
            /// <param name="code">User's input of this captcha.</param>
            /// <param name="cancellationToken">CancellationToken.</param>
            /// <returns></returns>
            public Task VerifyAsync(string code)
            {
                return AVCloud.VerifyCaptchaAsync(code, Token);
            }
        }

        /// <summary>
        /// Get a captcha image.
        /// </summary>
        /// <param name="width">captcha image width.</param>
        /// <param name="height">captcha image height.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>an instance of Captcha.</returns>
        public static Task<Captcha> RequestCaptchaAsync(int width = 85, int height = 30, CancellationToken cancellationToken = default)
        {
            var path = string.Format("requestCaptcha?width={0}&height={1}", width, height);
            var command = new AVCommand(path, "GET", null, data: null);
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var decoded = AVDecoder.Instance.Decode(t.Result.Item2) as IDictionary<string, object>;
                return new Captcha()
                {
                    Token = decoded["captcha_token"] as string,
                    Url = decoded["captcha_url"] as string,
                };
            });
        }

        /// <summary>
        /// Verify the user's input of catpcha.
        /// </summary>
        /// <param name="token">The captcha's token, from server.</param>
        /// <param name="code">User's input of this captcha.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns></returns>
        public static Task<string> VerifyCaptchaAsync(string code, string token, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>
            {
                { "captcha_token", token },
                { "captcha_code", code },
            };
            var command = new AVCommand("verifyCaptcha", "POST", null, data: data);
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).ContinueWith(t =>
            {
                if (!t.Result.Item2.ContainsKey("validate_token"))
                    throw new KeyNotFoundException("validate_token");
                return t.Result.Item2["validate_token"] as string;
            });
        }

        /// <summary>
        /// Get the custom cloud parameters, you can set them at console https://leancloud.cn/dashboard/devcomponent.html?appid={your_app_Id}#/component/custom_param
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<IDictionary<string, object>> GetCustomParametersAsync(CancellationToken cancellationToken = default)
        {
            var command = new AVCommand(string.Format("statistics/apps/{0}/sendPolicy", AVClient.CurrentConfiguration.ApplicationId),
               method: "GET",
               sessionToken: null,
               data: null);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var settings = t.Result.Item2;
                var CloudParameters = settings["parameters"] as IDictionary<string, object>;
                return CloudParameters;
            });
        }

        public class RealtimeSignature
        {
            public string Nonce { internal set; get; }
            public long Timestamp { internal set; get; }
            public string ClientId { internal set; get; }
            public string Signature { internal set; get; }
        }

        public static Task<RealtimeSignature> RequestRealtimeSignatureAsync(CancellationToken cancellationToken = default)
        {
            return AVUser.GetCurrentUserAsync(cancellationToken).OnSuccess(t =>
            {
                return RequestRealtimeSignatureAsync(t.Result, cancellationToken);
            }).Unwrap();
        }

        public static Task<RealtimeSignature> RequestRealtimeSignatureAsync(AVUser user, CancellationToken cancellationToken = default)
        {
            var command = new AVCommand(string.Format("rtm/sign"),
                    method: "POST",
                    sessionToken: null,
                    data: new Dictionary<string, object>
                    {
                        { "session_token", user.SessionToken },
                    }
                );
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).ContinueWith(t =>
            {
                var body = t.Result.Item2;
                return new RealtimeSignature()
                {
                    Nonce = body["nonce"] as string,
                    Timestamp = (long)body["timestamp"],
                    ClientId = body["client_id"] as string,
                    Signature = body["signature"] as string,
                };
            });
        }

        /// <summary>
        /// Gets the LeanEngine hosting URL for current app async.
        /// </summary>
        /// <returns>The lean engine hosting URL async.</returns>
        public static Task<string> GetLeanEngineHostingUrlAsync()
        {
            return CallFunctionAsync<string>("_internal_extensions_get_domain");
        }

    }


    /// <summary>
    /// AVRPCC loud function base.
    /// </summary>
    public class AVRPCCloudFunctionBase<P, R>
    {
        /// <summary>
        /// AVRPCD eserialize.
        /// </summary>
        public delegate R AVRPCDeserialize<R>(IDictionary<string, object> result);
        /// <summary>
        /// AVRPCS erialize.
        /// </summary>
        public delegate IDictionary<string, object> AVRPCSerialize<P>(P parameters);

        public AVRPCCloudFunctionBase()
            : this(true)
        {

        }

        public AVRPCCloudFunctionBase(bool noneParameters)
        {
            if (noneParameters)
            {
                this.Encode = n =>
                {
                    return null;
                };
            }
        }



        private AVRPCDeserialize<R> _decode;
        public AVRPCDeserialize<R> Decode
        {
            get
            {
                return _decode;
            }
            set
            {
                _decode = value;
            }
        }


        private AVRPCSerialize<P> _encode;
        public AVRPCSerialize<P> Encode
        {
            get
            {
                if (_encode == null)
                {
                    _encode = n =>
                    {
                        if (n != null)
                            return Json.Parse(n.ToString()) as IDictionary<string, object>;
                        return null;
                    };
                }
                return _encode;
            }
            set
            {
                _encode = value;
            }

        }
        public string FunctionName { get; set; }

        public Task<R> ExecuteAsync(P parameters)
        {
            return AVUser.GetCurrentAsync().OnSuccess(t =>
            {
                var user = t.Result;
                var encodedParameters = Encode(parameters);
                var command = new AVCommand(
                    string.Format("call/{0}", Uri.EscapeUriString(FunctionName)),
                    method: "POST",
                    sessionToken: user != null ? user.SessionToken : null,
                    data: encodedParameters);

                return AVClient.RunCommandAsync(command);

            }).Unwrap().OnSuccess(s =>
            {
                var responseBody = s.Result.Item2;
                if (!responseBody.ContainsKey("result"))
                {
                    return default(R);
                }

                return Decode(responseBody);
            });

        }
    }


    public class AVObjectRPCCloudFunction : AVObjectRPCCloudFunction<AVObject>
    {

    }
    public class AVObjectListRPCCloudFunction : AVObjectListRPCCloudFunction<AVObject>
    {

    }

    public class AVObjectListRPCCloudFunction<R> : AVRPCCloudFunctionBase<object, IList<R>> where R : AVObject
    {
        public AVObjectListRPCCloudFunction()
            : base(true)
        {
            this.Decode = this.AVObjectListDeserializer();
        }

        public AVRPCDeserialize<IList<R>> AVObjectListDeserializer()
        {
            AVRPCDeserialize<IList<R>> del = data =>
            {
                var items = data["result"] as IList<object>;

                return items.Select(item =>
                {
                    var state = AVObjectCoder.Instance.Decode(item as IDictionary<string, object>, AVDecoder.Instance);
                    return AVObject.FromState<AVObject>(state, state.ClassName);
                }).ToList() as IList<R>;

            };
            return del;
        }
    }

    public class AVObjectRPCCloudFunction<R> : AVRPCCloudFunctionBase<object, R> where R : AVObject
    {
        public AVObjectRPCCloudFunction()
            : base(true)
        {
            this.Decode = this.AVObjectDeserializer();
        }


        public AVRPCDeserialize<R> AVObjectDeserializer()
        {
            AVRPCDeserialize<R> del = data =>
            {
                var item = data["result"] as object;
                var state = AVObjectCoder.Instance.Decode(item as IDictionary<string, object>, AVDecoder.Instance);

                return AVObject.FromState<R>(state, state.ClassName);
            };
            return del;

        }
    }
}
