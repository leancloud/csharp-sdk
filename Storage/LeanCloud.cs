using System;
using LeanCloud.Storage;
using LeanCloud.Storage.Internal.Http;

namespace LeanCloud {
    /// <summary>
    /// LeanCloud 全局接口
    /// </summary>
    public class LeanCloud {
        // SDK 版本号，用于 User-Agent 统计
        internal const string SDKVersion = "0.1.0";

        // 接口版本号，用于接口版本管理
        internal const string APIVersion = "1.1";

        public static bool UseProduction {
            get; set;
        }

        internal static LCHttpClient HttpClient {
            get; private set;
        }

        public static void Initialize(string appId, string appKey, string server = null) {
            if (string.IsNullOrEmpty(appId)) {
                throw new ArgumentException(nameof(appId));
            }
            if (string.IsNullOrEmpty(appKey)) {
                throw new ArgumentException(nameof(appKey));
            }
            // 注册 LeanCloud 内部子类化类型
            LCObject.RegisterSubclass<LCUser>(LCUser.CLASS_NAME, () => new LCUser());
            LCObject.RegisterSubclass<LCRole>(LCRole.CLASS_NAME, () => new LCRole());
            LCObject.RegisterSubclass<LCFile>(LCFile.CLASS_NAME, () => new LCFile());

            HttpClient = new LCHttpClient(appId, appKey, server, SDKVersion, APIVersion);
        }
    }
}
