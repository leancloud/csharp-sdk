using System.Collections.Generic;
using System.Threading;
using LeanCloud.Storage;

namespace LeanCloud.Engine {
    /// <summary>
    /// LCEngineRequestContext provides the context of engine request.
    /// </summary>
    public class LCEngineRequestContext {
        public const string RemoteAddressKey = "__remoteAddressKey";
        public const string SessionTokenKey = "__sessionToken";
        public const string CurrentUserKey = "__currentUser";

        private static ThreadLocal<Dictionary<string, object>> requestContext = new ThreadLocal<Dictionary<string, object>>();

        public static void Init() {
            if (requestContext.IsValueCreated) {
                requestContext.Value.Clear();
            }
            requestContext.Value = new Dictionary<string, object>();
        }

        public static void Set(string key, object value) {
            if (!requestContext.IsValueCreated) {
                requestContext.Value = new Dictionary<string, object>();
            }
            requestContext.Value[key] = value;
        }

        public static object Get(string key) {
            if (!requestContext.IsValueCreated) {
                return null;
            }
            return requestContext.Value[key];
        }

        /// <summary>
        /// The remote address of this request.
        /// </summary>
        public static string RemoteAddress {
            get {
                object remoteAddress = Get(RemoteAddressKey);
                if (remoteAddress != null) {
                    return remoteAddress as string;
                }
                return null;
            }
            set {
                Set(RemoteAddressKey, value);
            }
        }

        /// <summary>
        /// The session token of this request.
        /// </summary>
        public static string SessionToken {
            get {
                object sessionToken = Get(SessionTokenKey);
                if (sessionToken != null) {
                    return sessionToken as string;
                }
                return null;
            }
            set {
                Set(SessionTokenKey, value);
            }
        }

        /// <summary>
        /// The user of this request.
        /// </summary>
        public static LCUser CurrentUser {
            get {
                object currentUser = Get(CurrentUserKey);
                if (currentUser != null) {
                    return currentUser as LCUser;
                }
                return null;
            }
            set {
                Set(CurrentUserKey, value);
            }
        }
    }
}
