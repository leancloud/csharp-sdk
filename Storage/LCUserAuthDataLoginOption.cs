/// <summary>
/// 第三方登录选项
/// </summary>
namespace LeanCloud.Storage {
    public class LCUserAuthDataLoginOption {
        /// <summary>
        /// Union Id 平台
        /// </summary>
        public string UnionIdPlatform {
            get; set;
        }

        /// <summary>
        /// 是否作为主账号
        /// </summary>
        public bool AsMainAccount {
            get; set;
        }

        /// <summary>
        /// 是否在不存在的情况下返回失败
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
