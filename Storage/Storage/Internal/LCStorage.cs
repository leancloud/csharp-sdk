using LeanCloud.Common;

namespace LeanCloud.Storage.Internal {
    public class LCStorage {
        private const string SessionHeaderKey = "X-LC-Session";

        public static void Initialize(string appId,
            string appKey,
            string server = null,
            string masterKey = null) {

            LCCore.Initialize(appId, appKey, server, masterKey);

            LCCore.HttpClient.AddRuntimeHeaderTask(SessionHeaderKey, async () => {
                LCUser currentUser = await LCUser.GetCurrent();
                if (currentUser == null) {
                    return null;
                }
                return currentUser.SessionToken;
            });

            // 注册 LeanCloud 内部子类化类型
            LCObject.RegisterSubclass(LCUser.CLASS_NAME, () => new LCUser(), LCUser.ENDPOINT);
            LCObject.RegisterSubclass(LCRole.CLASS_NAME, () => new LCRole(), LCRole.ENDPOINT);
            LCObject.RegisterSubclass(LCFile.CLASS_NAME, () => new LCFile(), LCFile.ENDPOINT);
            LCObject.RegisterSubclass(LCStatus.CLASS_NAME, () => new LCStatus(), LCStatus.ENDPOINT);
            LCObject.RegisterSubclass(LCFriendshipRequest.CLASS_NAME, () => new LCFriendshipRequest(), LCFriendshipRequest.ENDPOINT);
            LCObject.RegisterSubclass(LCInstallation.CLASS_NAME, () => new LCInstallation(), LCInstallation.ENDPOINT);
            LCObject.RegisterSubclass("_Follower", () => new LCObject("_Follower"), "followers");
            LCObject.RegisterSubclass("_Followee", () => new LCObject("_Followee"), "followees");
        }
    }
}
