
namespace LeanCloud.Storage.Internal {
    public class AVPlugins {
        private static readonly object instanceMutex = new object();
        private static AVPlugins instance;
        public static AVPlugins Instance {
            get {
                lock (instanceMutex) {
                    instance = instance ?? new AVPlugins();
                    return instance;
                }
            }
        }

        private readonly object mutex = new object();

        #region Server Controllers

        private HttpClient httpClient;
        private AppRouterController appRouterController;
        private AVCommandRunner commandRunner;
        private StorageController storageController;

        private AVCloudCodeController cloudCodeController;
        private AVFileController fileController;
        private AVObjectController objectController;
        private AVQueryController queryController;
        private AVSessionController sessionController;
        private AVUserController userController;
        private ObjectSubclassingController subclassingController;

        #endregion

        #region Current Instance Controller

        private AVCurrentUserController currentUserController;
        private InstallationIdController installationIdController;

        #endregion

        public void Reset() {
            lock (mutex) {
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

        public HttpClient HttpClient {
            get {
                lock (mutex) {
                    httpClient = httpClient ?? new HttpClient();
                    return httpClient;
                }
            }
            set {
                lock (mutex) {
                    httpClient = value;
                }
            }
        }

        public AppRouterController AppRouterController {
            get {
                lock (mutex) {
                    appRouterController = appRouterController ?? new AppRouterController();
                    return appRouterController;
                }
            }
            set {
                lock (mutex) {
                    appRouterController = value;
                }
            }
        }

        public AVCommandRunner CommandRunner {
            get {
                lock (mutex) {
                    commandRunner = commandRunner ?? new AVCommandRunner();
                    return commandRunner;
                }
            }
            set {
                lock (mutex) {
                    commandRunner = value;
                }
            }
        }

#if UNITY
        public StorageController StorageController {
            get {
                lock (mutex) {
                    storageController = storageController ?? new StorageController(AVInitializeBehaviour.IsWebPlayer, AVClient.CurrentConfiguration.ApplicationId);
                    return storageController;
                }
            }
            set {
                lock (mutex) {
                    storageController = value;
                }
            }
        }
#else
        public StorageController StorageController {
            get {
                lock (mutex) {
                    storageController = storageController ?? new StorageController(AVClient.CurrentConfiguration.ApplicationId);
                    return storageController;
                }
            }
            set {
                lock (mutex) {
                    storageController = value;
                }
            }
        }
#endif

        public AVCloudCodeController CloudCodeController {
            get {
                lock (mutex) {
                    cloudCodeController = cloudCodeController ?? new AVCloudCodeController();
                    return cloudCodeController;
                }
            }
            set {
                lock (mutex) {
                    cloudCodeController = value;
                }
            }
        }

        public AVFileController FileController {
            get {
                if (fileController != null) {
                    return fileController;
                }
                lock (mutex) {
                    switch (AVClient.CurrentConfiguration.RegionValue) {
                        case 0:
                            fileController = new QiniuFileController();
                            break;
                        case 2:
                            fileController = new QCloudCosFileController();
                            break;
                        case 1:
                            fileController = new AWSS3FileController();
                            break;
                    }
                    return fileController;
                }
            }
            set {
                lock (mutex) {
                    fileController = value;
                }
            }
        }

        public AVObjectController ObjectController {
            get {
                lock (mutex) {
                    objectController = objectController ?? new AVObjectController();
                    return objectController;
                }
            }
            set {
                lock (mutex) {
                    objectController = value;
                }
            }
        }

        public AVQueryController QueryController {
            get {
                lock (mutex) {
                    if (queryController == null) {
                        queryController = new AVQueryController();
                    }
                    return queryController;
                }
            }
            set {
                lock (mutex) {
                    queryController = value;
                }
            }
        }

        public AVSessionController SessionController {
            get {
                lock (mutex) {
                    sessionController = sessionController ?? new AVSessionController();
                    return sessionController;
                }
            }
            set {
                lock (mutex) {
                    sessionController = value;
                }
            }
        }

        public AVUserController UserController {
            get {
                lock (mutex) {
                    userController = userController ?? new AVUserController();
                    return userController;
                }
            }
            set {
                lock (mutex) {
                    userController = value;
                }
            }
        }

        public AVCurrentUserController CurrentUserController {
            get {
                lock (mutex) {
                    currentUserController = currentUserController ?? new AVCurrentUserController();
                    return currentUserController;
                }
            }
            set {
                lock (mutex) {
                    currentUserController = value;
                }
            }
        }

        public ObjectSubclassingController SubclassingController {
            get {
                lock (mutex) {
                    if (subclassingController == null) {
                        subclassingController = new ObjectSubclassingController();
                        subclassingController.AddRegisterHook(typeof(AVUser), () => CurrentUserController.ClearFromMemory());
                    }
                    return subclassingController;
                }
            }
            set {
                lock (mutex) {
                    subclassingController = value;
                }
            }
        }

        public InstallationIdController InstallationIdController {
            get {
                lock (mutex) {
                    installationIdController = installationIdController ?? new InstallationIdController();
                    return installationIdController;
                }
            }
            set {
                lock (mutex) {
                    installationIdController = value;
                }
            }
        }
    }
}
