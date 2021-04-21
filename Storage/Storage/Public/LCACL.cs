using System;
using System.Collections.Generic;

namespace LeanCloud.Storage {
    /// <summary>
    /// LCACL is used to control which users and roles can access or modify
    /// a particular object. Each LCObject can have its own LCACL.
    /// </summary>
    public class LCACL {
        const string PublicKey = "*";

        const string RoleKeyPrefix = "role:";

        internal Dictionary<string, bool> ReadAccess {
            get;
        } = new Dictionary<string, bool>();

        internal Dictionary<string, bool> WriteAccess {
            get;
        } = new Dictionary<string, bool>();

        /// <summary>
        /// Creates a LCACL that is allowed to read and write this object
        /// for the user.
        /// </summary>
        /// <param name="owner">The user.</param>
        /// <returns></returns>
        public static LCACL CreateWithOwner(LCUser owner) {
            if (owner == null) {
                throw new ArgumentNullException(nameof(owner));
            }
            LCACL acl = new LCACL();
            acl.SetUserReadAccess(owner, true);
            acl.SetUserWriteAccess(owner, true);
            return acl;
        }

        /// <summary>
        /// Gets or sets whether everyone is allowed to read this object.
        /// </summary>
        public bool PublicReadAccess {
            get {
                return GetAccess(ReadAccess, PublicKey);
            } set {
                SetAccess(ReadAccess, PublicKey, value);
            }
        }

        /// <summary>
        /// Gets or sets whether everyone is allowed to write this object.
        /// </summary>
        public bool PublicWriteAccess {
            get {
                return GetAccess(WriteAccess, PublicKey);
            } set {
                SetAccess(WriteAccess, PublicKey, value);
            }
        }

        /// <summary>
        /// Detects whether the given user id is *explicitly* allowed to read this
        /// object. Even if this returns false, the user may still be able to read
        /// it if <see cref="PublicReadAccess"/> is true or a role that the user
        /// belongs to has read access.
        /// </summary>
        /// <param name="userId">The user ObjectId to check.</param>
        /// <returns></returns>
        public bool GetUserIdReadAccess(string userId) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            return GetAccess(ReadAccess, userId);
        }

        /// <summary>
        /// Set whether the given user id is allowed to read this object.
        /// </summary>
        /// <param name="userId">The ObjectId of the user.</param>
        /// <param name="value">Whether the user has permission.</param>
        public void SetUserIdReadAccess(string userId, bool value) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            SetAccess(ReadAccess, userId, value);
        }

        /// <summary>
        /// Detects whether the given user id is *explicitly* allowed to write this
        /// object. Even if this returns false, the user may still be able to write
        /// it if <see cref="PublicWriteAccess"/> is true or a role that the user
        /// belongs to has read access.
        /// </summary>
        /// <param name="userId">T
        public bool GetUserIdWriteAccess(string userId) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            return GetAccess(WriteAccess, userId);
        }

        /// <summary>
        /// Set whether the given user id is allowed to write this object.
        /// </summary>
        /// <param name="userId">The ObjectId of the user.</param>
        /// <param name="value">Whether the user has permission.</param>
        public void SetUserIdWriteAccess(string userId, bool value) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            SetAccess(WriteAccess, userId, value);
        }

        /// <summary>
        /// Gets whether the given user is *explicitly* allowed to read this
        /// object. Even if this returns false, the user may still be able to read
        /// it if <see cref="PublicReadAccess"/> is true or a role that the user
        /// belongs to has read access.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <returns></returns>
        public bool GetUserReadAccess(LCUser user) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            return GetUserIdReadAccess(user.ObjectId);
        }

        /// <summary>
        /// Set whether the given user is allowed to read this object.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="value">Whether the user has permission.</param>
        public void SetUserReadAccess(LCUser user, bool value) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            SetUserIdReadAccess(user.ObjectId, value);
        }

        /// <summary>
        /// Gets whether the given user is *explicitly* allowed to write this
        /// object. Even if this returns false, the user may still be able to write
        /// it if <see cref="PublicReadAccess"/> is true or a role that the user
        /// belongs to has write access.
        /// </summary>
        /// <param name="userId">The user to check.</param>
        /// <returns></returns>
        public bool GetUserWriteAccess(LCUser user) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            return GetUserIdWriteAccess(user.ObjectId);
        }

        /// <summary>
        /// Set whether the given user is allowed to write this object.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="value">Whether the user has permission.</param>
        public void SetUserWriteAccess(LCUser user, bool value) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            SetUserIdWriteAccess(user.ObjectId, value);
        }

        /// <summary>
        /// Get whether the given role are allowed to read this object.
        /// </summary>
        /// <param name="role">The role to check.</param>
        /// <returns></returns>
        public bool GetRoleReadAccess(LCRole role) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            string roleKey = $"{RoleKeyPrefix}{role.ObjectId}";
            return GetAccess(ReadAccess, roleKey);
        }

        /// <summary>
        /// Sets whether the given role are allowed to read this object.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="value">Whether the role has access.</param>
        public void SetRoleReadAccess(LCRole role, bool value) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            string roleKey = $"{RoleKeyPrefix}{role.ObjectId}";
            SetAccess(ReadAccess, roleKey, value);
        }

        /// <summary>
        /// Get whether the given role are allowed to write this object.
        /// </summary>
        /// <param name="role">The role to check.</param>
        /// <returns></returns>
        public bool GetRoleWriteAccess(LCRole role) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            string roleKey = $"{RoleKeyPrefix}{role.ObjectId}";
            return GetAccess(WriteAccess, roleKey);
        }

        /// <summary>
        /// Sets whether the given role are allowed to write this object.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="value">Whether the role has access.</param>
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
