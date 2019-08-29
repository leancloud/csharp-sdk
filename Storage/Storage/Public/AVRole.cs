using System;
using System.Text.RegularExpressions;

namespace LeanCloud {
    /// <summary>
    /// 角色类
    /// </summary>
    [AVClassName("_Role")]
    public class AVRole : AVObject {
        private static readonly Regex namePattern = new Regex("^[0-9a-zA-Z_\\- ]+$");

        /// <summary>
        /// Constructs a new AVRole. You must assign a name and ACL to the role.
        /// </summary>
        public AVRole() { }

        /// <summary>
        /// Constructs a new AVRole with the given name.
        /// </summary>
        /// <param name="name">The name of the role to create.</param>
        /// <param name="acl">The ACL for this role. Roles must have an ACL.</param>
        public AVRole(string name, AVACL acl)
          : this() {
            Name = name;
            ACL = acl;
        }

        /// <summary>
        /// Gets the name of the role.
        /// </summary>
        [AVFieldName("name")]
        public string Name {
            get {
                return GetProperty<string>("Name");
            }
            set {
                SetProperty(value, "Name");
            }
        }

        /// <summary>
        /// Gets the <see cref="AVRelation{AVUser}"/> for the <see cref="AVUser"/>s that are
        /// direct children of this role. These users are granted any privileges that
        /// this role has been granted (e.g. read or write access through ACLs). You can
        /// add or remove child users from the role through this relation.
        /// </summary>
        [AVFieldName("users")]
        public AVRelation<AVUser> Users {
            get {
                return GetRelationProperty<AVUser>("Users");
            }
        }

        /// <summary>
        /// Gets the <see cref="AVRelation{AVRole}"/> for the <see cref="AVRole"/>s that are
        /// direct children of this role. These roles' users are granted any privileges that
        /// this role has been granted (e.g. read or write access through ACLs). You can
        /// add or remove child roles from the role through this relation.
        /// </summary>
        [AVFieldName("roles")]
        public AVRelation<AVRole> Roles {
            get {
                return GetRelationProperty<AVRole>("Roles");
            }
        }

        internal override void OnSettingValue(ref string key, ref object value) {
            base.OnSettingValue(ref key, ref value);
            if (key == "name") {
                if (ObjectId != null) {
                    throw new InvalidOperationException(
                        "A role's name can only be set before it has been saved.");
                }
                if (!(value is string)) {
                    throw new ArgumentException("A role's name must be a string.", nameof(value));
                }
                if (!namePattern.IsMatch((string)value)) {
                    throw new ArgumentException(
                        "A role's name can only contain alphanumeric characters, _, -, and spaces.", nameof(value));
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="AVQuery{AVRole}"/> over the Role collection.
        /// </summary>
        public static AVQuery<AVRole> Query {
            get {
                return new AVQuery<AVRole>();
            }
        }
    }
}
