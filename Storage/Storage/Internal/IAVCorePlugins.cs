using LeanCloud.Storage.Internal;
using System;

namespace LeanCloud.Storage.Internal
{
    public interface IAVCorePlugins
    {
        void Reset();

        IHttpClient HttpClient { get; }
        IAppRouterController AppRouterController { get; }
        IAVCommandRunner CommandRunner { get; }
        IStorageController StorageController { get; }

        IAVCloudCodeController CloudCodeController { get; }
        IAVConfigController ConfigController { get; }
        IAVFileController FileController { get; }
        IAVObjectController ObjectController { get; }
        IAVQueryController QueryController { get; }
        IAVSessionController SessionController { get; }
        IAVUserController UserController { get; }
        IObjectSubclassingController SubclassingController { get; }
        IAVCurrentUserController CurrentUserController { get; }
        IInstallationIdController InstallationIdController { get; }
    }
}