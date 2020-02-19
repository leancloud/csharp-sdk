using LeanCloud.Common;

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

        private AppRouterController appRouterController;
        private AVCommandRunner commandRunner;

        private AVCloudCodeController cloudCodeController;
        private AVFileController fileController;
        private AVQueryController queryController;
        private AVUserController userController;
        private ObjectSubclassingController subclassingController;

        #endregion

        #region Current Instance Controller

        private InstallationIdController installationIdController;

        #endregion

        public void Reset() {
            lock (mutex) {
                AppRouterController = null;
                CommandRunner = null;

                CloudCodeController = null;
                FileController = null;
                UserController = null;
                SubclassingController = null;

                InstallationIdController = null;
            }
        }

        public AppRouterController AppRouterController {
            get {
                lock (mutex) {
                    var conf = AVClient.CurrentConfiguration;
                    appRouterController = appRouterController ?? new AppRouterController(conf.ApplicationId, conf.ApiServer);
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
                fileController = new AVFileController();
                return fileController;
            }
            set {
                lock (mutex) {
                    fileController = value;
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

        public ObjectSubclassingController SubclassingController {
            get {
                lock (mutex) {
                    if (subclassingController == null) {
                        subclassingController = new ObjectSubclassingController();
                        //subclassingController.AddRegisterHook(typeof(AVUser), () => CurrentUserController.ClearFromMemory());
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
