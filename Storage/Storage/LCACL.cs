using System;
using System.Collections.Generic;

namespace LeanCloud.Storage {
    /// <summary>
    /// 访问控制类
    /// </summary>
    public class LCACL {
        const string PublicKey = "*";

        const string RoleKeyPrefix = "role:";

        internal HashSet<string> readers;
        internal HashSet<string> writers;

        public bool PublicReadAccess {
            get {
                return GetAccess(readers, PublicKey);
            } set {
                SetAccess(readers, PublicKey, value);
            }
        }

        public bool PublicWriteAccess {
            get {
                return GetAccess(writers, PublicKey);
            } set {
                SetAccess(writers, PublicKey, value);
            }
        }

        public bool GetUserIdReadAccess(string userId) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            return GetAccess(readers, userId);
        }

        public void SetUserIdReadAccess(string userId, bool value) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            SetAccess(readers, userId, value);
        }

        public bool GetUserIdWriteAccess(string userId) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            return GetAccess(writers, userId);
        }

        public void SetUserIdWriteAccess(string userId, bool value) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            SetAccess(writers, userId, value);
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
            return GetAccess(readers, roleKey);
        }

        public void SetRoleReadAccess(LCRole role, bool value) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            string roleKey = $"{RoleKeyPrefix}{role.ObjectId}";
            SetAccess(readers, roleKey, value);
        }

        public bool GetRoleWriteAccess(LCRole role) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            string roleKey = $"{RoleKeyPrefix}{role.ObjectId}";
            return GetAccess(writers, roleKey);
        }

        public void SetRoleWriteAccess(LCRole role, bool value) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            string roleKey = $"{RoleKeyPrefix}{role.ObjectId}";
            SetAccess(writers, roleKey, value);
        }

        public LCACL() {
            readers = new HashSet<string>();
            writers = new HashSet<string>();
        }

        bool GetAccess(HashSet<string> set, string key) {
            return set.Contains(key);
        }

        void SetAccess(HashSet<string> set, string key, bool value) {
            if (value) {
                set.Add(key);
            } else {
                set.Remove(key);
            }
        }
    }
}
