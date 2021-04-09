using System;
using System.Collections.Generic;

namespace LeanCloud.Storage {
    /// <summary>
    /// LeanCloud Access Control Lists.
    /// </summary>
    public class LCACL {
        const string PublicKey = "*";

        const string RoleKeyPrefix = "role:";

        public Dictionary<string, bool> ReadAccess {
            get;
        } = new Dictionary<string, bool>();

        public Dictionary<string, bool> WriteAccess {
            get;
        } = new Dictionary<string, bool>();

        public static LCACL CreateWithOwner(LCUser owner) {
            if (owner == null) {
                throw new ArgumentNullException(nameof(owner));
            }
            LCACL acl = new LCACL();
            acl.SetUserReadAccess(owner, true);
            acl.SetUserWriteAccess(owner, true);
            return acl;
        }

        public bool PublicReadAccess {
            get {
                return GetAccess(ReadAccess, PublicKey);
            } set {
                SetAccess(ReadAccess, PublicKey, value);
            }
        }

        public bool PublicWriteAccess {
            get {
                return GetAccess(WriteAccess, PublicKey);
            } set {
                SetAccess(WriteAccess, PublicKey, value);
            }
        }

        public bool GetUserIdReadAccess(string userId) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            return GetAccess(ReadAccess, userId);
        }

        public void SetUserIdReadAccess(string userId, bool value) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            SetAccess(ReadAccess, userId, value);
        }

        public bool GetUserIdWriteAccess(string userId) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            return GetAccess(WriteAccess, userId);
        }

        public void SetUserIdWriteAccess(string userId, bool value) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            SetAccess(WriteAccess, userId, value);
        }

        public bool GetUserReadAccess(LCUser user) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            return GetUserIdReadAccess(user.ObjectId);
        }

        public void SetUserReadAccess(LCUser user, bool value) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            SetUserIdReadAccess(user.ObjectId, value);
        }

        public bool GetUserWriteAccess(LCUser user) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            return GetUserIdWriteAccess(user.ObjectId);
        }

        public void SetUserWriteAccess(LCUser user, bool value) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            SetUserIdWriteAccess(user.ObjectId, value);
        }

        public bool GetRoleReadAccess(LCRole role) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            string roleKey = $"{RoleKeyPrefix}{role.ObjectId}";
            return GetAccess(ReadAccess, roleKey);
        }

        public void SetRoleReadAccess(LCRole role, bool value) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            string roleKey = $"{RoleKeyPrefix}{role.ObjectId}";
            SetAccess(ReadAccess, roleKey, value);
        }

        public bool GetRoleWriteAccess(LCRole role) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            string roleKey = $"{RoleKeyPrefix}{role.ObjectId}";
            return GetAccess(WriteAccess, roleKey);
        }

        public void SetRoleWriteAccess(LCRole role, bool value) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            string roleKey = $"{RoleKeyPrefix}{role.ObjectId}";
            SetAccess(WriteAccess, roleKey, value);
        }

        bool GetAccess(Dictionary<string, bool> access, string key) {
            return access.ContainsKey(key) ?
                access[key] : false;
        }

        void SetAccess(Dictionary<string, bool> access, string key, bool value) {
            access[key] = value;
        }
    }
}
