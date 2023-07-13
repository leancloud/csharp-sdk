using System;
using System.Collections.Generic;

namespace LeanCloud.Common {
    /// <summary>
    /// LeanCloud Application
    /// </summary>
    public class LCCore {
        // SDK 名字，用于 User-Agent 统计
        public const string SDKName = "LeanCloud-CSharp-SDK";

        // SDK 版本号，用于 User-Agent 统计
        public const string SDKVersion = "2.0.0";

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

        public static void Initialize(string appId,
            string appKey,
            string server = null,
            string masterKey = null) {
            if (string.IsNullOrEmpty(appId)) {
                throw new ArgumentNullException(nameof(appId));
            }
            if (string.IsNullOrEmpty(appKey)) {
                throw new ArgumentNullException(nameof(appKey));
            }

            AppId = appId;
            AppKey = appKey;
            MasterKey = masterKey;

            AppRouter = new LCAppRouter(appId, server);

            HttpClient = new LCHttpClient(appId, appKey, server, SDKVersion, APIVersion);
        }
    }
}
