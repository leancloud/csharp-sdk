using System;

namespace LeanCloud {
    /// <summary>
    /// AuthData 登陆选项
    /// </summary>
    public class AVUserAuthDataLogInOption {

        /// <summary>
        /// unionId platform
        /// </summary>
        /// <value>unionId platform.</value>
        public string UnionIdPlatform;

        /// <summary>
        /// If true, the unionId will be associated with the user.
        /// </summary>
        /// <value><c>true</c> If true, the unionId will be associated with the user. <c>false</c>.</value>
        public bool AsMainAccount;

        /// <summary>
        /// If true, the login request will fail when no user matches this authData exists.
        /// </summary>
        /// <value><c>true</c> If true, the login request will fail when no user matches this authData exists. <c>false</c>.</value>
        public bool FailOnNotExist;

        public AVUserAuthDataLogInOption() {
            UnionIdPlatform = "weixin";
            AsMainAccount = false;
            FailOnNotExist = false;
        }
    }
}
