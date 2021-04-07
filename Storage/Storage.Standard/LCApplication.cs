using LeanCloud.Storage.Internal.Storage;

namespace LeanCloud {
    public class LCApplication {
        public static void Initialize(string appId,
            string appKey,
            string server = null,
            string masterKey = null) {
            LCLogger.Debug("Application Initializes on Standard.");

            LCInternalApplication.Initialize(appId, appKey, server, masterKey);

            LCInternalApplication.StorageController = new StorageController(null);
        }
    }
}
