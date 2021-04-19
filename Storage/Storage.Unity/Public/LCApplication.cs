using LeanCloud.Common;
using LeanCloud.Storage;
using LeanCloud.Storage.Internal.Persistence;

namespace LeanCloud {
    /// <summary>
    /// LCApplication contains static functions that handle global configuration
    /// for LeanCloud services.
    /// </summary>
    public class LCApplication {
        /// <summary>
        /// Whether or not ths SDK using master key.
        /// Default is false.
        /// </summary>
        public static bool UseMasterKey {
            get => LCCore.UseMasterKey;
            set {
                LCCore.UseMasterKey = value;
            }
        }

        /// <summary>
        /// Initialize LeanCloud services.
        /// </summary>
        /// <param name="appId">The application id provided in LeanCloud dashboard.</param>
        /// <param name="appKey">The application key provided in LeanCloud dashboard.</param>
        /// <param name="server">The server url bound by yourself.</param>
        /// <param name="masterKey">The application master key provided in LeanCloud dashboard.</param>
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
