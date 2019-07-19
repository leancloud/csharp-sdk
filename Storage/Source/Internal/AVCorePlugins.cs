using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Storage.Internal
{
    public class AVPlugins : IAVCorePlugins
    {
        private static readonly object instanceMutex = new object();
        private static IAVCorePlugins instance;
        public static IAVCorePlugins Instance
        {
            get
            {
                lock (instanceMutex)
                {
                    instance = instance ?? new AVPlugins();
                    return instance;
                }
            }
            set
            {
                lock (instanceMutex)
                {
                    instance = value;
                }
            }
        }

        private readonly object mutex = new object();

        #region Server Controllers

        private IHttpClient httpClient;
        private IAppRouterController appRouterController;
        private IAVCommandRunner commandRunner;
        private IStorageController storageController;

        private IAVCloudCodeController cloudCodeController;
        private IAVConfigController configController;
        private IAVFileController fileController;
        private IAVObjectController objectController;
        private IAVQueryController queryController;
        private IAVSessionController sessionController;
        private IAVUserController userController;
        private IObjectSubclassingController subclassingController;

        #endregion

        #region Current Instance Controller

        private IAVCurrentUserController currentUserController;
        private IInstallationIdController installationIdController;

        #endregion

        public void Reset()
        {
            lock (mutex)
            {
                HttpClient = null;
                AppRouterController = null;
                CommandRunner = null;
                StorageController = null;

                CloudCodeController = null;
                FileController = null;
                ObjectController = null;
                SessionController = null;
                UserController = null;
                SubclassingController = null;

                CurrentUserController = null;
                InstallationIdController = null;
            }
        }

        public IHttpClient HttpClient
        {
            get
            {
                lock (mutex)
                {
                    httpClient = httpClient ?? new HttpClient();
                    return httpClient;
                }
            }
            set
            {
                lock (mutex)
                {
                    httpClient = value;
                }
            }
        }

        public IAppRouterController AppRouterController
        {
            get
            {
                lock (mutex)
                {
                    appRouterController = appRouterController ?? new AppRouterController();
                    return appRouterController;
                }
            }
            set
            {
                lock (mutex)
                {
                    appRouterController = value;
                }
            }
        }

        public IAVCommandRunner CommandRunner
        {
            get
            {
                lock (mutex)
                {
                    commandRunner = commandRunner ?? new AVCommandRunner(HttpClient, InstallationIdController);
                    return commandRunner;
                }
            }
            set
            {
                lock (mutex)
                {
                    commandRunner = value;
                }
            }
        }

#if !UNITY
        public IStorageController StorageController
        {
            get
            {
                lock (mutex)
                {
                    storageController = storageController ?? new StorageController(AVClient.CurrentConfiguration.ApplicationId);
                    return storageController;
                }
            }
            set
            {
                lock (mutex)
                {
                    storageController = value;
                }
            }
        }
#endif
#if UNITY
        public IStorageController StorageController
        {
            get
            {
                lock (mutex)
                {
                    storageController = storageController ?? new StorageController(AVInitializeBehaviour.IsWebPlayer, AVClient.CurrentConfiguration.ApplicationId);
                    return storageController;
                }
            }
            set
            {
                lock (mutex)
                {
                    storageController = value;
                }
            }
        }
#endif

        public IAVCloudCodeController CloudCodeController
        {
            get
            {
                lock (mutex)
                {
                    cloudCodeController = cloudCodeController ?? new AVCloudCodeController(CommandRunner);
                    return cloudCodeController;
                }
            }
            set
            {
                lock (mutex)
                {
                    cloudCodeController = value;
                }
            }
        }

        public IAVFileController FileController
        {
            get
            {
                lock (mutex)
                {
                    if (AVClient.CurrentConfiguration.RegionValue == 0)
                        fileController = fileController ?? new QiniuFileController(CommandRunner);
                    else if (AVClient.CurrentConfiguration.RegionValue == 2)
                        fileController = fileController ?? new QCloudCosFileController(CommandRunner);
                    else if (AVClient.CurrentConfiguration.RegionValue == 1)
                        fileController = fileController ?? new AWSS3FileController(CommandRunner);

                    return fileController;
                }
            }
            set
            {
                lock (mutex)
                {
                    fileController = value;
                }
            }
        }

        public IAVConfigController ConfigController
        {
            get
            {
                lock (mutex)
                {
                    if (configController == null)
                    {
                        configController = new AVConfigController(CommandRunner, StorageController);
                    }
                    return configController;
                }
            }
            set
            {
                lock (mutex)
                {
                    configController = value;
                }
            }
        }

        public IAVObjectController ObjectController
        {
            get
            {
                lock (mutex)
                {
                    objectController = objectController ?? new AVObjectController(CommandRunner);
                    return objectController;
                }
            }
            set
            {
                lock (mutex)
                {
                    objectController = value;
                }
            }
        }

        public IAVQueryController QueryController
        {
            get
            {
                lock (mutex)
                {
                    if (queryController == null)
                    {
                        queryController = new AVQueryController(CommandRunner);
                    }
                    return queryController;
                }
            }
            set
            {
                lock (mutex)
                {
                    queryController = value;
                }
            }
        }

        public IAVSessionController SessionController
        {
            get
            {
                lock (mutex)
                {
                    sessionController = sessionController ?? new AVSessionController(CommandRunner);
                    return sessionController;
                }
            }
            set
            {
                lock (mutex)
                {
                    sessionController = value;
                }
            }
        }

        public IAVUserController UserController
        {
            get
            {
                lock (mutex)
                {
                    userController = userController ?? new AVUserController(CommandRunner);
                    return userController;
                }
            }
            set
            {
                lock (mutex)
                {
                    userController = value;
                }
            }
        }

        public IAVCurrentUserController CurrentUserController
        {
            get
            {
                lock (mutex)
                {
                    currentUserController = currentUserController ?? new AVCurrentUserController(StorageController);
                    return currentUserController;
                }
            }
            set
            {
                lock (mutex)
                {
                    currentUserController = value;
                }
            }
        }

        public IObjectSubclassingController SubclassingController
        {
            get
            {
                lock (mutex)
                {
                    if (subclassingController == null)
                    {
                        subclassingController = new ObjectSubclassingController();
                        subclassingController.AddRegisterHook(typeof(AVUser), () => CurrentUserController.ClearFromMemory());
                    }
                    return subclassingController;
                }
            }
            set
            {
                lock (mutex)
                {
                    subclassingController = value;
                }
            }
        }

        public IInstallationIdController InstallationIdController
        {
            get
            {
                lock (mutex)
                {
                    installationIdController = installationIdController ?? new InstallationIdController(StorageController);
                    return installationIdController;
                }
            }
            set
            {
                lock (mutex)
                {
                    installationIdController = value;
                }
            }
        }
    }
}
