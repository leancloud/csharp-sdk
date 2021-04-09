using LeanCloud.Common;
using LeanCloud.Storage;
using LeanCloud.Storage.Internal.Persistence;

namespace LeanCloud {
    public class LCApplication {
        public static bool UseMasterKey {
            get => LCCore.UseMasterKey;
            set {
                LCCore.UseMasterKey = value;
            }
        }

        public static void Initialize(string appId,
            string appKey,
            string server = null,
            string masterKey = null) {
            LCLogger.Debug("Application Initializes on Unity.");

            LCStorage.Initialize(appId, appKey, server, masterKey);

            LCCore.PersistenceController = new PersistenceController(new UnityPersistence());
        }
    }
}
