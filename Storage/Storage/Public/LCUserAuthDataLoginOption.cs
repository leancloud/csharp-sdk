namespace LeanCloud.Storage {
    /// <summary>
    /// LCUser UnionID login parameters.
    /// </summary>
    public class LCUserAuthDataLoginOption {
        /// <summary>
        /// The platform of the UnionID.
        /// </summary>
        public string UnionIdPlatform {
            get; set;
        }

        /// <summary>
        /// Whether the current authentication information will be used as the main account.
        /// </summary>
        public bool AsMainAccount {
            get; set;
        }

        /// <summary>
        /// Whether the login request will fail if no user matching this authData exists.
        /// </summary>
        public bool FailOnNotExist {
            get; set;
        }

        public LCUserAuthDataLoginOption() {
            UnionIdPlatform = "weixin";
            AsMainAccount = false;
            FailOnNotExist = false;
        }
    }
}
