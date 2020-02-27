namespace LeanCloud.Storage {
    /// <summary>
    /// 角色
    /// </summary>
    public class LCRole : LCObject {
        public const string CLASS_NAME = "_Role";

        /// <summary>
        /// 名字
        /// </summary>
        public string Name {
            get {
                return this["name"] as string;
            } set {
                this["name"] = value;
            }
        }

        /// <summary>
        /// 关联角色
        /// </summary>
        public LCRelation<LCRole> Roles {
            get {
                LCRelation<LCObject> roles = this["roles"] as LCRelation<LCObject>;
                return new LCRelation<LCRole> {
                    Parent = roles.Parent,
                    Key = "roles"
                };
            }
        }

        /// <summary>
        /// 关联用户
        /// </summary>
        public LCRelation<LCUser> Users {
            get {
                LCRelation<LCObject> users = this["users"] as LCRelation<LCObject>;
                return new LCRelation<LCUser> {
                    Parent = users.Parent,
                    Key = "users"
                };
            }
        }

        public LCRole() : base(CLASS_NAME) {
        }

        public static LCRole Create(string name, LCACL acl) {
            LCRole role = new LCRole() {
                Name = name,
                ACL = acl
            };
            return role;
        }

        /// <summary>
        /// 获取角色查询对象
        /// </summary>
        /// <returns></returns>
        public static LCQuery<LCRole> GetQuery() {
            return new LCQuery<LCRole>(CLASS_NAME);
        }
    }
}
