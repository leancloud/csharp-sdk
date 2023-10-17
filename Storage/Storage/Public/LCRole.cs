namespace LeanCloud.Storage {
    /// <summary>
    /// LeanCloud Role, a group of users for the purposes of granting permissions.
    /// </summary>
    public class LCRole : LCObject {
        public const string CLASS_NAME = "_Role";
        public const string ENDPOINT = "roles";

        /// <summary>
        /// The name of a LCRole.
        /// </summary>
        public string Name {
            get {
                return this["name"] as string;
            } set {
                this["name"] = value;
            }
        }

        /// <summary>
        /// Child roles.
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
        /// Child users.
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

        /// <summary>
        /// Constructs a LCRole.
        /// </summary>
        public LCRole() : base(CLASS_NAME) {
        }

        /// <summary>
        /// Constructs a LCRole with a name and a LCACL.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="acl"></param>
        /// <returns></returns>
        public static LCRole Create(string name, LCACL acl) {
            LCRole role = new LCRole() {
                Name = name,
                ACL = acl
            };
            return role;
        }

        /// <summary>
        /// Constructs a LCQuery for this role.
        /// </summary>
        /// <returns></returns>
        public static LCQuery<LCRole> GetQuery() {
            return new LCQuery<LCRole>(CLASS_NAME);
        }
    }
}
