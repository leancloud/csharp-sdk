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
    /// AVClient contains static functions that handle global
    /// configuration for the LeanCloud library.
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
        /// Represents the configuration of the LeanCloud SDK.
        /// </summary>
        public struct Configuration {
            /// <summary>
            /// 与 SDK 通讯的云端节点 
            /// </summary>
            public enum AVRegion {
                /// <summary>
                /// 默认值，LeanCloud 华北节点，同 Public_North_China
                /// </summary>
                [Obsolete("please use Configuration.AVRegion.Public_North_China")]
                Public_CN = 0,

                /// <summary>
                /// 默认值，华北公有云节点，同 Public_CN
                /// </summary>
                Public_North_China = 0,

                /// <summary>
                /// LeanCloud 北美区公有云节点，同 Public_North_America
                /// </summary>
                [Obsolete("please use Configuration.AVRegion.Public_North_America")]
                Public_US = 1,
                /// <summary>
                /// LeanCloud 北美区公有云节点，同 Public_US
                /// </summary>
                Public_North_America = 1,

                /// <summary>
                /// 华东公有云节点，同 Public_East_China
                /// </summary>
                [Obsolete("please use Configuration.AVRegion.Public_East_China")]
                Vendor_Tencent = 2,

                /// <summary>
                /// 华东公有云节点，同 Vendor_Tencent
                /// </summary>
                Public_East_China = 2,
            }

            /// <summary>
            /// In the event that you would like to use the LeanCloud SDK
            /// from a completely portable project, with no platform-specific library required,
            /// to get full access to all of our features available on LeanCloud.com
            /// (A/B testing, slow queries, etc.), you must set the values of this struct
            /// to be appropriate for your platform.
            ///
            /// Any values set here will overwrite those that are automatically configured by
            /// any platform-specific migration library your app includes.
            /// </summary>
            public struct VersionInformation {
                /// <summary>
                /// The build number of your app.
                /// </summary>
                public String BuildVersion { get; set; }

                /// <summary>
                /// The human friendly version number of your happ.
                /// </summary>
                public String DisplayVersion { get; set; }

                /// <summary>
                /// The operating system version of the platform the SDK is operating in..
                /// </summary>
                public String OSVersion { get; set; }

            }

            /// <summary>
            /// The LeanCloud application ID of your app.
            /// </summary>
            public string ApplicationId { get; set; }

            /// <summary>
            /// LeanCloud  C# SDK 支持的服务节点，目前支持华北，华东和北美公有云节点和私有节点，以及专属节点
            /// </summary>
            public AVRegion Region { get; set; }

            internal int RegionValue {
                get {
                    return (int)Region;
                }
            }

            /// <summary>
            /// The LeanCloud application key for your app.
            /// </summary>
            public string ApplicationKey { get; set; }

            /// <summary>
            /// The LeanCloud master key for your app.
            /// </summary>
            /// <value>The master key.</value>
            public string MasterKey { get; set; }

            /// <summary>
            /// Gets or sets additional HTTP headers to be sent with network requests from the SDK.
            /// </summary>
            public IDictionary<string, string> AdditionalHTTPHeaders { get; set; }

            /// <summary>
            /// The version information of your application environment.
            /// </summary>
            public VersionInformation VersionInfo { get; set; }

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
        /// The current configuration that LeanCloud has been initialized with.
        /// </summary>
        public static Configuration CurrentConfiguration { get; internal set; }

        internal static string APIVersion {
            get {
                return "1.1";
            }
        }

        public static string Name {
            get {
                return "LeanCloud-CSharp-SDK";
            }
        }

        /// <summary>
        /// 当前 SDK 版本号
        /// </summary>
        public static string Version {
            get {
                return "0.1.0";
            }
        }

        /// <summary>
        /// Authenticates this client as belonging to your application. This must be
        /// called before your application can use the LeanCloud library. The recommended
        /// way is to put a call to <c>AVClient.Initialize</c> in your
        /// Application startup.
        /// </summary>
        /// <param name="applicationId">The Application ID provided in the LeanCloud dashboard.
        /// </param>
        /// <param name="applicationKey">The .NET API Key provided in the LeanCloud dashboard.
        /// </param>
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
        /// Gets or sets a value indicating whether send the request to production server or staging server.
        /// </summary>
        /// <value><c>true</c> if use production; otherwise, <c>false</c>.</value>
        public static bool UseProduction {
            get; set;
        }

        public static bool UseMasterKey {
            get; set;
        }

        /// <summary>
        /// Authenticates this client as belonging to your application. This must be
        /// called before your application can use the LeanCloud library. The recommended
        /// way is to put a call to <c>AVClient.Initialize</c> in your
        /// Application startup.
        /// </summary>
        /// <param name="configuration">The configuration to initialize LeanCloud with.
        /// </param>
        public static void Initialize(Configuration configuration) {
            Config(configuration);

            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVRole>();
        }

        internal static void Config(Configuration configuration) {
            lock (mutex) {
                var nodeHash = configuration.ApplicationId.Split('-');
                if (nodeHash.Length > 1) {
                    if (nodeHash[1].Trim() == "9Nh9j0Va") {
                        configuration.Region = Configuration.AVRegion.Public_East_China;
                    }
                }

                CurrentConfiguration = configuration;
            }
        }

        internal static void Clear() {
            AVPlugins.Instance.AppRouterController.Clear();
            AVPlugins.Instance.Reset();
        }

        /// <summary>
        /// Switch app.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static void Switch(Configuration configuration) {
            Clear();
            Initialize(configuration);
        }

        public static void Switch(string applicationId, string applicationKey, Configuration.AVRegion region = Configuration.AVRegion.Public_North_China) {
            var configuration = new Configuration {
                ApplicationId = applicationId,
                ApplicationKey = applicationKey,
                Region = region
            };
            Switch(configuration);
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
