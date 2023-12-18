using System;
using LeanCloud.Common;
using LeanCloud.Storage.Internal;

namespace LeanCloud {
    public class LCApplication {
        public static bool UseMasterKey {
            get => LCCore.UseMasterKey;
            set {
                LCCore.UseMasterKey = value;
            }
        }

        public static TimeSpan RequestTimeout {
            get => LCCore.RequestTimeout;
            set {
                LCCore.RequestTimeout = value;
            }
        }

        public static void Initialize(string appId,
            string appKey,
            string server = null,
            string masterKey = null) {
            LCLogger.Debug("Application Initializes on Standard.");

            LCStorage.Initialize(appId, appKey, server, masterKey);

            LCCore.PersistenceController = new PersistenceController(null);
        }
    }
}
