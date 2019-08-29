using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LeanCloud {
    /// <summary>
    /// LeanCloud SDK 客户端类
    /// </summary>
    public static class AVClient {
        public static readonly string[] DateFormatStrings = {
            // Official ISO format
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'",
            // It's possible that the string converter server-side may trim trailing zeroes,
            // so these two formats cover ourselves from that.
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ff'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'f'Z'",
        };

        /// <summary>
        /// LeanCloud SDK 配置
        /// </summary>
        public struct Configuration {
            /// <summary>
            /// App Id
            /// </summary>
            public string ApplicationId { get; set; }

            /// <summary>
            /// App Key
            /// </summary>
            public string ApplicationKey { get; set; }

            /// <summary>
            /// Master Key
            /// </summary>
            public string MasterKey { get; set; }

            /// <summary>
            /// Gets or sets additional HTTP headers to be sent with network requests from the SDK.
            /// </summary>
            public IDictionary<string, string> AdditionalHTTPHeaders { get; set; }

            /// <summary>
            /// 存储服务器地址
            /// </summary>
            /// <value>The API server.</value>
            public string ApiServer { get; set; }

            /// <summary>
            /// 云引擎服务器地址
            /// </summary>
            /// <value>The engine server uri.</value>
            public string EngineServer { get; set; }

            /// <summary>
            /// 即时通信服务器地址
            /// </summary>
            /// <value>The RTMR outer.</value>
            public string RTMServer { get; set; }

            /// <summary>
            /// 直连即时通信服务器地址
            /// </summary>
            /// <value>The realtime server.</value>
            public string RealtimeServer { get; set; }

            public Uri PushServer { get; set; }

            public Uri StatsServer { get; set; }
        }

        private static readonly object mutex = new object();

        static AVClient() {
        }

        /// <summary>
        /// LeanCloud SDK 当前配置
        /// </summary>
        public static Configuration CurrentConfiguration { get; internal set; }

        internal static string APIVersion {
            get {
                return "1.1";
            }
        }

        internal static string Name {
            get {
                return "LeanCloud-CSharp-SDK";
            }
        }

        internal static string Version {
            get {
                return "0.1.0";
            }
        }

        /// <summary>
        /// 初始化 LeanCloud SDK
        /// </summary>
        /// <param name="applicationId">App Id</param>
        /// <param name="applicationKey">App Key</param>
        public static void Initialize(string applicationId, string applicationKey) {
            Initialize(new Configuration {
                ApplicationId = applicationId,
                ApplicationKey = applicationKey
            });
        }

        internal static Action<string> LogTracker { get; private set; }

        /// <summary>
        /// 启动日志打印
        /// </summary>
        /// <param name="trace"></param>
        public static void HttpLog(Action<string> trace) {
            LogTracker = trace;
        }
        /// <summary>
        /// 打印 HTTP 访问日志
        /// </summary>
        /// <param name="log"></param>
        public static void PrintLog(string log) {
            LogTracker?.Invoke(log);
        }

        /// <summary>
        /// 是否使用生产环境
        /// </summary>
        public static bool UseProduction {
            get; set;
        }

        /// <summary>
        /// 是否使用 MasterKey
        /// </summary>
        public static bool UseMasterKey {
            get; set;
        }

        /// <summary>
        /// 初始化 LeanCloud
        /// </summary>
        /// <param name="configuration">初始化配置</param>
        public static void Initialize(Configuration configuration) {
            CurrentConfiguration = configuration;

            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVRole>();
        }

        internal static void Clear() {
            AVPlugins.Instance.AppRouterController.Clear();
            AVPlugins.Instance.Reset();
        }

        public static string BuildQueryString(IDictionary<string, object> parameters) {
            return string.Join("&", (from pair in parameters
                                     let valueString = pair.Value as string
                                     select string.Format("{0}={1}",
                                       Uri.EscapeDataString(pair.Key),
                                       Uri.EscapeDataString(string.IsNullOrEmpty(valueString) ?
                                          JsonConvert.SerializeObject(pair.Value) : valueString)))
                                       .ToArray());
        }

        internal static IDictionary<string, string> DecodeQueryString(string queryString) {
            var dict = new Dictionary<string, string>();
            foreach (var pair in queryString.Split('&')) {
                var parts = pair.Split(new char[] { '=' }, 2);
                dict[parts[0]] = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace("+", " ")) : null;
            }
            return dict;
        }
    }
}
