using System;
using System.Collections.Generic;

namespace LeanCloud.Common {
    /// <summary>
    /// LeanCloud Application
    /// </summary>
    public class LCCore {
        // SDK 版本号，用于 User-Agent 统计
        public const string SDKVersion = "0.7.3";

        // 接口版本号，用于接口版本管理
        public const string APIVersion = "1.1";

        public static string AppId {
            get; private set;
        }

        public static string AppKey {
            get; private set;
        }

        public static string MasterKey {
            get; private set;
        }

        public static bool UseProduction {
            get; set;
        }

        public static LCAppRouter AppRouter {
            get; private set;
        }

        public static LCHttpClient HttpClient {
            get; private set;
        }

        public static bool UseMasterKey {
            get; set;
        }

        public static PersistenceController PersistenceController {
            get; set;
        }

        internal static Dictionary<string, string> AdditionalHeaders {
            get;
        } = new Dictionary<string, string>();

        public static void Initialize(string appId,
            string appKey,
            string server = null,
            string masterKey = null) {
            if (string.IsNullOrEmpty(appId)) {
                throw new ArgumentException(nameof(appId));
            }
            if (string.IsNullOrEmpty(appKey)) {
                throw new ArgumentException(nameof(appKey));
            }

            AppId = appId;
            AppKey = appKey;
            MasterKey = masterKey;

            AppRouter = new LCAppRouter(appId, server);

            HttpClient = new LCHttpClient(appId, appKey, server, SDKVersion, APIVersion);
        }

        public static void AddHeader(string key, string value) {
            AdditionalHeaders.Add(key, value);
        }
    }
}
