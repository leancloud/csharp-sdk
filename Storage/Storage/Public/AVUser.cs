using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud {
    /// <summary>
    /// Represents a user for a LeanCloud application.
    /// </summary>
    [AVClassName("_User")]
    public class AVUser : AVObject {
        private static readonly IDictionary<string, IAVAuthenticationProvider> authProviders =
            new Dictionary<string, IAVAuthenticationProvider>();

        private static readonly HashSet<string> readOnlyKeys = new HashSet<string> {
            "sessionToken", "isNew"
        };

        internal static AVUserController UserController {
            get {
                return AVPlugins.Instance.UserController;
            }
        }

        /// <summary>
        /// Whether the AVUser has been authenticated on this device. Only an authenticated
        /// AVUser can be saved and deleted.
        /// </summary>
        [Obsolete("This property is deprecated, please use IsAuthenticatedAsync instead.")]
        public bool IsAuthenticated {
            get {
                lock (mutex) {
                    return SessionToken != null &&
                      CurrentUser != null &&
                      CurrentUser.ObjectId == ObjectId;
                }
            }
        }

        public bool IsCurrent {
            get {
                return CurrentUser == this;
            }
        }

        /// <summary>
        /// Whether the AVUser has been authenticated on this device, and the AVUser's session token is expired.
        /// Only an authenticated AVUser can be saved and deleted.
        /// </summary>
        public Task IsAuthenticatedAsync() {
            lock (mutex) {
                if (SessionToken == null || CurrentUser == null || CurrentUser.ObjectId != ObjectId) {
                    return Task.FromResult(false);
                }
            }
            var command = new AVCommand {
                Path = $"users/me?session_token={CurrentSessionToken}",
                Method = HttpMethod.Get
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }

        /// <summary>
        /// Refresh this user's session token, and current session token will be invalid.
        /// </summary>
        public Task RefreshSessionTokenAsync(CancellationToken cancellationToken) {
            return UserController.RefreshSessionTokenAsync(ObjectId, SessionToken, cancellationToken).OnSuccess(t => {
                var serverState = t.Result;
                HandleSave(serverState);
            });
        }

        /// <summary>
        /// authenticated token.
        /// </summary>
        public string SessionToken {
            get {
                if (State.ContainsKey("sessionToken")) {
                    return State["sessionToken"] as string;
                }
                return null;
            }
        }

        internal static string CurrentSessionToken {
            get {
                return CurrentUser?.SessionToken;
            }
        }

        internal void SetSessionTokenAsync(string newSessionToken) {
            SetSessionTokenAsync(newSessionToken, CancellationToken.None);
        }

        internal void SetSessionTokenAsync(string newSessionToken, CancellationToken cancellationToken) {
            MutateState(mutableClone => {
                mutableClone.ServerData["sessionToken"] = newSessionToken;
            });

            CurrentUser = this;
        }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [AVFieldName("username")]
        public string Username {
            get { return GetProperty<string>(null, "Username"); }
            set { SetProperty(value, "Username"); }
        }

        /// <summary>
        /// Sets the password.
        /// </summary>
        [AVFieldName("password")]
        public string Password {
            private get { return GetProperty<string>(null, "Password"); }
            set { SetProperty(value, "Password"); }
        }

        /// <summary>
        /// Sets the email address.
        /// </summary>
        [AVFieldName("email")]
        public string Email {
            get { return GetProperty<string>(null, "Email"); }
            set { SetProperty(value, "Email"); }
        }

        /// <summary>
        /// 用户手机号。
        /// </summary>
        [AVFieldName("mobilePhoneNumber")]
        public string MobilePhoneNumber {
            get {
                return GetProperty<string>(null, "MobilePhoneNumber");
            }
            set {
                SetProperty<string>(value, "MobilePhoneNumber");
            }
        }

        /// <summary>
        /// 用户手机号是否已经验证
        /// </summary>
        /// <value><c>true</c> if mobile phone verified; otherwise, <c>false</c>.</value>
        [AVFieldName("mobilePhoneVerified")]
        public bool MobilePhoneVerified {
            get {
                return GetProperty<bool>(false, "MobilePhoneVerified");
            }
            set {
                SetProperty<bool>(value, "MobilePhoneVerified");
            }
        }

        /// <summary>
        /// 判断用户是否为匿名用户
        /// </summary>
        public bool IsAnonymous {
            get {
                bool rtn = false;
                if (this.AuthData != null) {
                    rtn = this.AuthData.Keys.Contains("anonymous");
                }
                return rtn;
            }
        }

        internal async Task SignUpAsync(Task toAwait, CancellationToken cancellationToken) {
            await Create(toAwait, cancellationToken);
            CurrentUser = this;
        }

        /// <summary>
        /// Signs up a new user. This will create a new AVUser on the server and will also persist the
        /// session on disk so that you can access the user using <see cref="CurrentUser"/>. A username and
        /// password must be set before calling SignUpAsync.
        /// </summary>
        public Task SignUpAsync() {
            return SignUpAsync(CancellationToken.None);
        }

        /// <summary>
        /// Signs up a new user. This will create a new AVUser on the server and will also persist the
        /// session on disk so that you can access the user using <see cref="CurrentUser"/>. A username and
        /// password must be set before calling SignUpAsync.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task SignUpAsync(CancellationToken cancellationToken) {
            return taskQueue.Enqueue(toAwait => SignUpAsync(toAwait, cancellationToken),
                cancellationToken);
        }

        #region 事件流系统相关 API

        /// <summary>
        /// 关注某个用户
        /// </summary>
        /// <param name="userObjectId">被关注的用户</param>
        /// <returns></returns>
        public Task FollowAsync(string userObjectId) {
            return FollowAsync(userObjectId, null);
        }

        /// <summary>
        /// 关注某个用户
        /// </summary>
        /// <param name="userObjectId">被关注的用户Id</param>
        /// <param name="data">关注的时候附加属性</param>
        /// <returns></returns>
        public Task FollowAsync(string userObjectId, IDictionary<string, object> data) {
            if (data != null) {
                data = this.EncodeForSaving(data);
            }
            var command = new AVCommand {
                Path = $"users/{ObjectId}/friendship/{userObjectId}",
                Method = HttpMethod.Post,
                Content = data
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }

        /// <summary>
        /// 取关某一个用户
        /// </summary>
        /// <param name="userObjectId"></param>
        /// <returns></returns>
        public Task UnfollowAsync(string userObjectId) {
            var command = new AVCommand {
                Path = $"users/{ObjectId}/friendship/{userObjectId}",
                Method = HttpMethod.Delete
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }

        /// <summary>
        /// 获取当前用户的关注者的查询
        /// </summary>
        /// <returns></returns>
        public AVQuery<AVUser> GetFollowerQuery() {
            AVQuery<AVUser> query = new AVQuery<AVUser> {
                Path = $"users/{ObjectId}/followers"
            };
            return query;
        }

        /// <summary>
        /// 获取当前用户所关注的用户的查询
        /// </summary>
        /// <returns></returns>
        public AVQuery<AVUser> GetFolloweeQuery() {
            AVQuery<AVUser> query = new AVQuery<AVUser> {
                Path = $"users/{ObjectId}/followees"
            };
            return query;
        }

        /// <summary>
        /// 同时查询关注了当前用户的关注者和当前用户所关注的用户
        /// </summary>
        /// <returns></returns>
        public AVQuery<AVUser> GetFollowersAndFolloweesQuery() {
            AVQuery<AVUser> query = new AVQuery<AVUser> {
                Path = $"users/{ObjectId}/followersAndFollowees"
            };
            return query;
        }

        /// <summary>
        /// 获取当前用户的关注者
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<AVUser>> GetFollowersAsync() {
            return GetFollowerQuery().FindAsync(CancellationToken.None);
        }

        /// <summary>
        /// 获取当前用户所关注的用户
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<AVUser>> GetFolloweesAsync() {
            return GetFolloweeQuery().FindAsync(CancellationToken.None);
        }


        //public Task<AVStatus> SendStatusAsync()
        //{

        //}

        #endregion

        /// <summary>
        /// Logs in a user with a username and password. On success, this saves the session to disk so you
        /// can retrieve the currently logged in user using <see cref="CurrentUser"/>.
        /// </summary>
        /// <param name="username">The username to log in with.</param>
        /// <param name="password">The password to log in with.</param>
        /// <returns>The newly logged-in user.</returns>
        public static Task<AVUser> LogInAsync(string username, string password) {
            return LogInAsync(username, password, CancellationToken.None);
        }

        public static async Task<AVUser> LogInAsync(string username, string password, CancellationToken cancellationToken) {
            var ret = await UserController.LogInAsync(username, null, password, cancellationToken);
            AVUser user = FromState<AVUser>(ret, "_User");
            CurrentUser = user;
            return user;
        }

        /// <summary>
        /// Logs in a user with a username and password. On success, this saves the session to disk so you
        /// can retrieve the currently logged in user using <see cref="CurrentUser"/>.
        /// </summary>
        /// <param name="sessionToken">The session token to authorize with</param>
        /// <returns>The user if authorization was successful</returns>
        public static Task<AVUser> BecomeAsync(string sessionToken) {
            return BecomeAsync(sessionToken, CancellationToken.None);
        }

        /// <summary>
        /// Logs in a user with a username and password. On success, this saves the session to disk so you
        /// can retrieve the currently logged in user using <see cref="CurrentUser"/>.
        /// </summary>
        /// <param name="sessionToken">The session token to authorize with</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user if authorization was successful</returns>
        public static async Task<AVUser> BecomeAsync(string sessionToken, CancellationToken cancellationToken) {
            var ret = await UserController.GetUserAsync(sessionToken, cancellationToken);
            AVUser user = FromState<AVUser>(ret, "_User");
            CurrentUser = user;
            return user;
        }

        /// <summary>
        /// Logs out the currently logged in user session. This will remove the session from disk, log out of
        /// linked services, and future calls to <see cref="CurrentUser"/> will return <c>null</c>.
        /// </summary>
        /// <remarks>
        /// Typically, you should use <see cref="LogOutAsync()"/>, unless you are managing your own threading.
        /// </remarks>
        public static void LogOut() {
            // TODO (hallucinogen): this will without a doubt fail in Unity. But what else can we do?
            //LogOutAsync().Wait();
        }

        /// <summary>
        /// Logs out the currently logged in user session. This will remove the session from disk, log out of
        /// linked services, and future calls to <see cref="CurrentUser"/> will return <c>null</c>.
        /// </summary>
        /// <remarks>
        /// This is preferable to using <see cref="LogOut()"/>, unless your code is already running from a
        /// background thread.
        /// </remarks>
        public static void LogOutAsync() {
            //return LogOutAsync(CancellationToken.None);
        }

        /// <summary>
        /// Logs out the currently logged in user session. This will remove the session from disk, log out of
        /// linked services, and future calls to <see cref="CurrentUser"/> will return <c>null</c>.
        ///
        /// This is preferable to using <see cref="LogOut()"/>, unless your code is already running from a
        /// background thread.
        /// </summary>
        public static void LogOutAsync(CancellationToken cancellationToken) {
            CurrentUser = null;
        }

        //internal Task LogOutAsync(Task toAwait, CancellationToken cancellationToken) {
        //    string oldSessionToken = SessionToken;
        //    if (oldSessionToken == null) {
        //        return Task.FromResult(0);
        //    }

        //    // Cleanup in-memory session.
        //    MutateState(mutableClone => {
        //        mutableClone.ServerData.Remove("sessionToken");
        //    });
        //    var revokeSessionTask = AVSession.RevokeAsync(oldSessionToken, cancellationToken);
        //    return Task.WhenAll(revokeSessionTask, CurrentUserController.LogOutAsync(cancellationToken));
        //}

        private static void LogOutWithProviders() {
            foreach (var provider in authProviders.Values) {
                provider.Deauthenticate();
            }
        }

        public static AVUser CurrentUser {
            // TODO 线程问题
            get; internal set;
        }

        /// <summary>
        /// Constructs a <see cref="AVQuery{AVUser}"/> for AVUsers.
        /// </summary>
        public static AVQuery<AVUser> Query {
            get {
                return new AVQuery<AVUser>();
            }
        }

        #region Legacy / Revocable Session Tokens

        private static readonly object isRevocableSessionEnabledMutex = new object();
        private static bool isRevocableSessionEnabled;

        internal static void DisableRevocableSession() {
            lock (isRevocableSessionEnabledMutex) {
                isRevocableSessionEnabled = false;
            }
        }

        internal static bool IsRevocableSessionEnabled {
            get {
                lock (isRevocableSessionEnabledMutex) {
                    return isRevocableSessionEnabled;
                }
            }
        }

        #endregion

        /// <summary>
        /// Requests a password reset email to be sent to the specified email address associated with the
        /// user account. This email allows the user to securely reset their password on the LeanCloud site.
        /// </summary>
        /// <param name="email">The email address associated with the user that forgot their password.</param>
        public static Task RequestPasswordResetAsync(string email) {
            return RequestPasswordResetAsync(email, CancellationToken.None);
        }

        /// <summary>
        /// Requests a password reset email to be sent to the specified email address associated with the
        /// user account. This email allows the user to securely reset their password on the LeanCloud site.
        /// </summary>
        /// <param name="email">The email address associated with the user that forgot their password.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task RequestPasswordResetAsync(string email,
            CancellationToken cancellationToken) {
            return UserController.RequestPasswordResetAsync(email, cancellationToken);
        }

        /// <summary>
        /// Updates current user's password. Need the user's old password,
        /// </summary>
        /// <returns>The password.</returns>
        /// <param name="oldPassword">Old password.</param>
        /// <param name="newPassword">New password.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task UpdatePassword(string oldPassword, string newPassword, CancellationToken cancellationToken) {
            return UserController.UpdatePasswordAsync(ObjectId, SessionToken, oldPassword, newPassword, cancellationToken);
        }

        /// <summary>
        /// Gets the authData for this user.
        /// </summary>
        internal IDictionary<string, IDictionary<string, object>> AuthData {
            get {
                IDictionary<string, IDictionary<string, object>> authData;
                if (this.TryGetValue<IDictionary<string, IDictionary<string, object>>>(
                    "authData", out authData)) {
                    return authData;
                }
                return null;
            }
            private set {
                this["authData"] = value;
            }
        }

        private static IAVAuthenticationProvider GetProvider(string providerName) {
            IAVAuthenticationProvider provider;
            if (authProviders.TryGetValue(providerName, out provider)) {
                return provider;
            }
            return null;
        }

        /// <summary>
        /// Removes null values from authData (which exist temporarily for unlinking)
        /// </summary>
        private void CleanupAuthData() {
            lock (mutex) {
                if (!IsCurrent) {
                    return;
                }
                var authData = AuthData;

                if (authData == null) {
                    return;
                }

                foreach (var pair in new Dictionary<string, IDictionary<string, object>>(authData)) {
                    if (pair.Value == null) {
                        authData.Remove(pair.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Synchronizes authData for all providers.
        /// </summary>
        private void SynchronizeAllAuthData() {
            lock (mutex) {
                var authData = AuthData;

                if (authData == null) {
                    return;
                }

                foreach (var pair in authData) {
                    SynchronizeAuthData(GetProvider(pair.Key));
                }
            }
        }

        private void SynchronizeAuthData(IAVAuthenticationProvider provider) {
            bool restorationSuccess = false;
            lock (mutex) {
                var authData = AuthData;
                if (authData == null || provider == null) {
                    return;
                }
                IDictionary<string, object> data;
                if (authData.TryGetValue(provider.AuthType, out data)) {
                    restorationSuccess = provider.RestoreAuthentication(data);
                }
            }

            if (!restorationSuccess) {
                this.UnlinkFromAsync(provider.AuthType, CancellationToken.None);
            }
        }

        public Task LinkWithAsync(string authType, IDictionary<string, object> data, CancellationToken cancellationToken) {
            return taskQueue.Enqueue(toAwait => {
                AuthData = new Dictionary<string, IDictionary<string, object>>();
                AuthData[authType] = data;
                return SaveAsync(cancellationToken);
            }, cancellationToken);
        }

        public Task LinkWithAsync(string authType, CancellationToken cancellationToken) {
            var provider = GetProvider(authType);
            return provider.AuthenticateAsync(cancellationToken)
              .OnSuccess(t => LinkWithAsync(authType, t.Result, cancellationToken))
              .Unwrap();
        }

        /// <summary>
        /// Unlinks a user from a service.
        /// </summary>
        public Task UnlinkFromAsync(string authType, CancellationToken cancellationToken) {
            return LinkWithAsync(authType, null, cancellationToken);
        }

        /// <summary>
        /// Checks whether a user is linked to a service.
        /// </summary>
        internal bool IsLinked(string authType) {
            lock (mutex) {
                return AuthData != null && AuthData.ContainsKey(authType) && AuthData[authType] != null;
            }
        }

        internal static async Task<AVUser> LogInWithAsync(string authType,
            IDictionary<string, object> data,
            bool failOnNotExist,
            CancellationToken cancellationToken) {
            
            var ret = await UserController.LogInAsync(authType, data, failOnNotExist, cancellationToken);
            AVUser user = FromState<AVUser>(ret, "_User");
            user.AuthData = new Dictionary<string, IDictionary<string, object>>();
            user.AuthData[authType] = data;
            user.SynchronizeAllAuthData();
            CurrentUser = user;
            return CurrentUser;
        }

        internal static Task<AVUser> LogInWithAsync(string authType,
            CancellationToken cancellationToken) {
            var provider = GetProvider(authType);
            return provider.AuthenticateAsync(cancellationToken)
              .OnSuccess(authData => LogInWithAsync(authType, authData.Result, false, cancellationToken))
              .Unwrap();
        }

        internal static void RegisterProvider(IAVAuthenticationProvider provider) {
            authProviders[provider.AuthType] = provider;
            var curUser = AVUser.CurrentUser;
            if (curUser != null) {
                curUser.SynchronizeAuthData(provider);
            }
        }

        #region 手机号登录

        internal static async Task<AVUser> LogInWithParametersAsync(Dictionary<string, object> strs, CancellationToken cancellationToken) {
            var ret = await UserController.LogInWithParametersAsync("login", strs, cancellationToken);
            var user = CreateWithoutData<AVUser>(null);
            user.HandleFetchResult(ret);
            CurrentUser = user;
            return CurrentUser;
        }

        /// <summary>
        /// 以手机号和密码实现登陆。
        /// </summary>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        public static Task<AVUser> LogInByMobilePhoneNumberAsync(string mobilePhoneNumber, string password) {
            return AVUser.LogInByMobilePhoneNumberAsync(mobilePhoneNumber, password, CancellationToken.None);
        }

        /// <summary>
        /// 以手机号和验证码匹配登陆
        /// </summary>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="smsCode">短信验证码</param>
        /// <returns></returns>
        public static Task<AVUser> LogInBySmsCodeAsync(string mobilePhoneNumber, string smsCode) {
            return AVUser.LogInBySmsCodeAsync(mobilePhoneNumber, smsCode, CancellationToken.None);
        }

        /// <summary>
        /// 用邮箱作和密码匹配登录
        /// </summary>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        public static async Task<AVUser> LogInByEmailAsync(string email, string password, CancellationToken cancellationToken = default(CancellationToken)) {
            var ret = await UserController.LogInAsync(null, email, password, cancellationToken);
            AVUser user = FromState<AVUser>(ret, "_User");
            CurrentUser = user;
            return CurrentUser;
        }


        /// <summary>
        /// 以手机号和密码匹配登陆
        /// </summary>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="password">密码</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<AVUser> LogInByMobilePhoneNumberAsync(string mobilePhoneNumber, string password, CancellationToken cancellationToken) {
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "mobilePhoneNumber", mobilePhoneNumber },
                { "password", password }
            };
            return AVUser.LogInWithParametersAsync(strs, cancellationToken);
        }

        /// <summary>
        /// 以手机号和验证码登陆
        /// </summary>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="smsCode">短信验证码</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<AVUser> LogInBySmsCodeAsync(string mobilePhoneNumber, string smsCode, CancellationToken cancellationToken = default(CancellationToken)) {
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "mobilePhoneNumber", mobilePhoneNumber },
                { "smsCode", smsCode }
            };
            return AVUser.LogInWithParametersAsync(strs, cancellationToken);
        }

        /// <summary>
        /// Requests the login SMS code asynchronous.
        /// </summary>
        /// <param name="mobilePhoneNumber">The mobile phone number.</param>
        /// <returns></returns>
        public static Task RequestLogInSmsCodeAsync(string mobilePhoneNumber) {
            return RequestLogInSmsCodeAsync(mobilePhoneNumber, CancellationToken.None);
        }

        /// <summary>
        /// Requests the login SMS code asynchronous.
        /// </summary>
        /// <param name="mobilePhoneNumber">The mobile phone number.</param>
        /// <param name="validateToken">Validate token.</param>
        /// <returns></returns>
        public static Task RequestLogInSmsCodeAsync(string mobilePhoneNumber, string validateToken) {
            return RequestLogInSmsCodeAsync(mobilePhoneNumber, null, CancellationToken.None);
        }

        /// <summary>
        /// Requests the login SMS code asynchronous.
        /// </summary>
        /// <param name="mobilePhoneNumber">The mobile phone number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task RequestLogInSmsCodeAsync(string mobilePhoneNumber, CancellationToken cancellationToken) {
            return RequestLogInSmsCodeAsync(mobilePhoneNumber, null, cancellationToken);
        }

        /// <summary>
        /// Requests the login SMS code asynchronous.
        /// </summary>
        /// <param name="mobilePhoneNumber">The mobile phone number.</param>
        /// <param name="validateToken">Validate token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task RequestLogInSmsCodeAsync(string mobilePhoneNumber, string validateToken, CancellationToken cancellationToken) {
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "mobilePhoneNumber", mobilePhoneNumber },
            };
            if (String.IsNullOrEmpty(validateToken)) {
                strs.Add("validate_token", validateToken);
            }
            var command = new AVCommand {
                Path = "requestLoginSmsCode",
                Method = HttpMethod.Post,
                Content = strs
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }

        /// <summary>
        /// 手机号一键登录
        /// </summary>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="smsCode">短信验证码</param>
        /// <returns></returns>
        public static async Task<AVUser> SignUpOrLogInByMobilePhoneAsync(string mobilePhoneNumber, string smsCode, CancellationToken cancellationToken) {
            Dictionary<string, object> strs = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobilePhoneNumber },
                { "smsCode", smsCode }
            };
            var ret = await UserController.LogInWithParametersAsync("usersByMobilePhone", strs, cancellationToken);
            var user = CreateWithoutData<AVUser>(null);
            user.HandleFetchResult(ret);
            CurrentUser = user;
            return CurrentUser;
        }

        /// <summary>
        /// 手机号一键登录
        /// </summary>
        /// <returns>signup or login by mobile phone async.</returns>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="smsCode">短信验证码</param>
        public static Task<AVUser> SignUpOrLogInByMobilePhoneAsync(string mobilePhoneNumber, string smsCode) {
            return AVUser.SignUpOrLogInByMobilePhoneAsync(mobilePhoneNumber, smsCode, CancellationToken.None);
        }

        #region mobile sms shortcode sign up & log in.
        /// <summary>
        /// Send sign up sms code async.
        /// </summary>
        /// <returns>The sign up sms code async.</returns>
        /// <param name="mobilePhoneNumber">Mobile phone number.</param>
        public static Task SendSignUpSmsCodeAsync(string mobilePhoneNumber) {
            return AVCloud.RequestSMSCodeAsync(mobilePhoneNumber);
        }

        /// <summary>
        /// Sign up by mobile phone async.
        /// </summary>
        /// <returns>The up by mobile phone async.</returns>
        /// <param name="mobilePhoneNumber">Mobile phone number.</param>
        /// <param name="smsCode">Sms code.</param>
        public static Task<AVUser> SignUpByMobilePhoneAsync(string mobilePhoneNumber, string smsCode) {
            return AVUser.SignUpOrLogInByMobilePhoneAsync(mobilePhoneNumber, smsCode);
        }

        /// <summary>
        /// Send log in sms code async.
        /// </summary>
        /// <returns>The log in sms code async.</returns>
        /// <param name="mobilePhoneNumber">Mobile phone number.</param>
        public static Task SendLogInSmsCodeAsync(string mobilePhoneNumber) {
            return AVUser.RequestLogInSmsCodeAsync(mobilePhoneNumber);
        }

        /// <summary>
        /// Log in by mobile phone async.
        /// </summary>
        /// <returns>The in by mobile phone async.</returns>
        /// <param name="mobilePhoneNumber">Mobile phone number.</param>
        /// <param name="smsCode">Sms code.</param>
        public static Task<AVUser> LogInByMobilePhoneAsync(string mobilePhoneNumber, string smsCode) {
            return AVUser.LogInBySmsCodeAsync(mobilePhoneNumber, smsCode);
        }
        #endregion
        #endregion

        #region 重置密码
        /// <summary>
        ///  请求重置密码，需要传入注册时使用的手机号。
        /// </summary>
        /// <param name="mobilePhoneNumber">注册时使用的手机号</param>
        /// <returns></returns>
        public static Task RequestPasswordResetBySmsCode(string mobilePhoneNumber) {
            return AVUser.RequestPasswordResetBySmsCode(mobilePhoneNumber, null, CancellationToken.None);
        }

        /// <summary>
        ///  请求重置密码，需要传入注册时使用的手机号。
        /// </summary>
        /// <param name="mobilePhoneNumber">注册时使用的手机号</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns></returns>
        public static Task RequestPasswordResetBySmsCode(string mobilePhoneNumber, CancellationToken cancellationToken) {
            return RequestPasswordResetBySmsCode(mobilePhoneNumber, null, cancellationToken);
        }

        /// <summary>
        ///  请求重置密码，需要传入注册时使用的手机号。
        /// </summary>
        /// <param name="mobilePhoneNumber">注册时使用的手机号</param>
        /// <param name="validateToken">Validate token.</param>
        /// <returns></returns>
        public static Task RequestPasswordResetBySmsCode(string mobilePhoneNumber, string validateToken) {
            return AVUser.RequestPasswordResetBySmsCode(mobilePhoneNumber, validateToken, CancellationToken.None);
        }

        /// <summary>
        ///  请求重置密码，需要传入注册时使用的手机号。
        /// </summary>
        /// <param name="mobilePhoneNumber">注册时使用的手机号</param>
        /// <param name="validateToken">Validate token.</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns></returns>
        public static Task RequestPasswordResetBySmsCode(string mobilePhoneNumber, string validateToken, CancellationToken cancellationToken) {
            string currentSessionToken = AVUser.CurrentSessionToken;
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "mobilePhoneNumber", mobilePhoneNumber },
            };
            if (String.IsNullOrEmpty(validateToken)) {
                strs.Add("validate_token", validateToken);
            }
            var command = new AVCommand {
                Path = "requestPasswordResetBySmsCode",
                Method = HttpMethod.Post,
                Content = strs
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }

        /// <summary>
        /// 通过验证码重置密码。
        /// </summary>
        /// <param name="newPassword">新密码</param>
        /// <param name="smsCode">6位数验证码</param>
        /// <returns></returns>
        public static Task ResetPasswordBySmsCodeAsync(string newPassword, string smsCode) {
            return ResetPasswordBySmsCodeAsync(newPassword, smsCode, CancellationToken.None);
        }

        /// <summary>
        /// 通过验证码重置密码。
        /// </summary>
        /// <param name="newPassword">新密码</param>
        /// <param name="smsCode">6位数验证码</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns></returns>
        public static Task ResetPasswordBySmsCodeAsync(string newPassword, string smsCode, CancellationToken cancellationToken) {
            string currentSessionToken = AVUser.CurrentSessionToken;
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "password", newPassword }
            };
            var command = new AVCommand {
                Path = $"resetPasswordBySmsCode/{smsCode}",
                Method = HttpMethod.Put,
                Content = strs
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }

        /// <summary>
        /// 发送认证码到需要认证的手机上
        /// </summary>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <returns></returns>
        public static Task RequestMobilePhoneVerifyAsync(string mobilePhoneNumber) {
            return RequestMobilePhoneVerifyAsync(mobilePhoneNumber, null, CancellationToken.None);
        }

        /// <summary>
        /// 发送认证码到需要认证的手机上
        /// </summary>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="validateToken">Validate token.</param>
        /// <returns></returns>
        public static Task RequestMobilePhoneVerifyAsync(string mobilePhoneNumber, string validateToken) {
            return RequestMobilePhoneVerifyAsync(mobilePhoneNumber, validateToken, CancellationToken.None);
        }

        /// <summary>
        /// 发送认证码到需要认证的手机上
        /// </summary>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns></returns>
        public static Task RequestMobilePhoneVerifyAsync(string mobilePhoneNumber, CancellationToken cancellationToken) {
            return RequestMobilePhoneVerifyAsync(mobilePhoneNumber, null, cancellationToken);
        }

        /// <summary>
        /// 发送认证码到需要认证的手机上
        /// </summary>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <param name="validateToken">Validate token.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns></returns>
        public static Task RequestMobilePhoneVerifyAsync(string mobilePhoneNumber, string validateToken, CancellationToken cancellationToken) {
            string currentSessionToken = AVUser.CurrentSessionToken;
            Dictionary<string, object> strs = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobilePhoneNumber }
            };
            if (!string.IsNullOrEmpty(validateToken)) {
                strs.Add("validate_token", validateToken);
            }
            var command = new AVCommand {
                Path = "requestMobilePhoneVerify",
                Method = HttpMethod.Post,
                Content = strs
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }

        /// <summary>
        /// 验证手机验证码是否为有效值
        /// </summary>
        /// <param name="code">手机收到的验证码</param>
        /// <param name="mobilePhoneNumber">手机号</param>
        /// <returns></returns>
        public static Task VerifyMobilePhoneAsync(string code, string mobilePhoneNumber) {
            return VerifyMobilePhoneAsync(code, mobilePhoneNumber, CancellationToken.None);
        }

        /// <summary>
        /// 验证手机验证码是否为有效值
        /// </summary>
        /// <param name="code">手机收到的验证码</param>
        /// <param name="mobilePhoneNumber">手机号，可选</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task VerifyMobilePhoneAsync(string code, string mobilePhoneNumber, CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = $"verifyMobilePhone/{code.Trim()}?mobilePhoneNumber={mobilePhoneNumber.Trim()}",
                Method = HttpMethod.Post
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }

        /// <summary>
        /// 验证手机验证码是否为有效值
        /// </summary>
        /// <param name="code">手机收到的验证码</param>
        /// <returns></returns>
        public static Task VerifyMobilePhoneAsync(string code) {
            var command = new AVCommand {
                Path = $"verifyMobilePhone/{code.Trim()}",
                Method = HttpMethod.Post
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }

        /// <summary>
        ///  验证手机验证码是否为有效值
        /// </summary>
        /// <param name="code">手机收到的验证码</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns></returns>
        public static Task<bool> VerifyMobilePhoneAsync(string code, CancellationToken cancellationToken) {
            return AVUser.VerifyMobilePhoneAsync(code, CancellationToken.None);
        }

        #endregion

        #region 邮箱验证
        /// <summary>
        /// 申请发送验证邮箱的邮件，一周之内有效
        /// 如果该邮箱已经验证通过，会直接返回 True，并不会真正发送邮件
        /// 注意，不能频繁的调用此接口，一天之内只允许向同一个邮箱发送验证邮件 3 次，超过调用次数，会直接返回错误
        /// </summary>
        /// <param name="email">邮箱地址</param>
        /// <returns></returns>
        public static Task RequestEmailVerifyAsync(string email) {
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "email", email }
            };
            var command = new AVCommand {
                Path = "requestEmailVerify",
                Method = HttpMethod.Post,
                Content = strs
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }
        #endregion

        #region in no-local-storage enviroment

        internal Task Create() {
            return this.Create(CancellationToken.None);
        }
        internal Task Create(CancellationToken cancellationToken) {
            return taskQueue.Enqueue(toAwait => Create(toAwait, cancellationToken),
               cancellationToken);
        }

        internal Task Create(Task toAwait, CancellationToken cancellationToken) {
            if (AuthData == null) {
                // TODO (hallucinogen): make an Extension of Task to create Task with exception/canceled.
                if (string.IsNullOrEmpty(Username)) {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    tcs.TrySetException(new InvalidOperationException("Cannot sign up user with an empty name."));
                    return tcs.Task;
                }
                if (string.IsNullOrEmpty(Password)) {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    tcs.TrySetException(new InvalidOperationException("Cannot sign up user with an empty password."));
                    return tcs.Task;
                }
            }
            if (!string.IsNullOrEmpty(ObjectId)) {
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                tcs.TrySetException(new InvalidOperationException("Cannot sign up a user that already exists."));
                return tcs.Task;
            }

            IDictionary<string, IAVFieldOperation> currentOperations = StartSave();

            return toAwait.OnSuccess(_ => {
                return UserController.SignUpAsync(State, currentOperations, cancellationToken);
            }).Unwrap().ContinueWith(t => {
                if (t.IsFaulted || t.IsCanceled) {
                    HandleFailedSave(currentOperations);
                } else {
                    var serverState = t.Result;
                    HandleSave(serverState);
                }
                return t;
            }).Unwrap();
        }
        #endregion

        #region AVUser Extension
        public IDictionary<string, IDictionary<string, object>> GetAuthData() {
            return AuthData;
        }

        /// <summary>
        /// use 3rd auth data to sign up or log in.if user with the same auth data exits,it will transfer as log in.
        /// </summary>
        /// <param name="data">OAuth data, like {"accessToken":"xxxxxx"}</param>
        /// <param name="platform">auth platform,maybe "facebook"/"twiiter"/"weibo"/"weixin" .etc</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<AVUser> LogInWithAuthDataAsync(IDictionary<string, object> data,
            string platform,
            AVUserAuthDataLogInOption options = null,
            CancellationToken cancellationToken = default(System.Threading.CancellationToken)) {
            if (options == null) {
                options = new AVUserAuthDataLogInOption();
            }
            return AVUser.LogInWithAsync(platform, data, options.FailOnNotExist, cancellationToken);
        }

        public static Task<AVUser> LogInWithAuthDataAndUnionIdAsync(
            IDictionary<string, object> authData,
            string platform,
            string unionId,
            AVUserAuthDataLogInOption options = null,
            CancellationToken cancellationToken = default(System.Threading.CancellationToken)) {
            if (options == null) {
                options = new AVUserAuthDataLogInOption();
            }
            MergeAuthData(authData, unionId, options);
            return AVUser.LogInWithAsync(platform, authData, options.FailOnNotExist, cancellationToken);
        }

        public static Task<AVUser> LogInAnonymouslyAsync(CancellationToken cancellationToken = default(System.Threading.CancellationToken)) {
            var data = new Dictionary<string, object> {
                { "id", Guid.NewGuid().ToString() }
            };
            var options = new AVUserAuthDataLogInOption();
            return LogInWithAuthDataAsync(data, "anonymous", options, cancellationToken);
        }

        [Obsolete("please use LogInWithAuthDataAsync instead.")]
        public static Task<AVUser> LogInWithAsync(string authType, IDictionary<string, object> data, CancellationToken cancellationToken) {
            return AVUser.LogInWithAsync(authType, data, false, cancellationToken);
        }

        /// <summary>
        /// link a 3rd auth account to the user.
        /// </summary>
        /// <param name="user">AVUser instance</param>
        /// <param name="data">OAuth data, like {"accessToken":"xxxxxx"}</param>
        /// <param name="platform">auth platform,maybe "facebook"/"twiiter"/"weibo"/"weixin" .etc</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task AssociateAuthDataAsync(IDictionary<string, object> data, string platform, CancellationToken cancellationToken = default(System.Threading.CancellationToken)) {
            return LinkWithAsync(platform, data, cancellationToken);
        }

        public Task AssociateAuthDataAndUnionIdAsync(
            IDictionary<string, object> authData,
            string platform,
            string unionId,
            AVUserAuthDataLogInOption options = null,
            CancellationToken cancellationToken = default(System.Threading.CancellationToken)) {
            if (options == null) {
                options = new AVUserAuthDataLogInOption();
            }
            MergeAuthData(authData, unionId, options);
            return LinkWithAsync(platform, authData, cancellationToken);
        }

        /// <summary>
        /// unlink a 3rd auth account from the user.
        /// </summary>
        /// <param name="user">AVUser instance</param>
        /// <param name="platform">auth platform,maybe "facebook"/"twiiter"/"weibo"/"weixin" .etc</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task DisassociateWithAuthDataAsync(string platform, CancellationToken cancellationToken = default(System.Threading.CancellationToken)) {
            return UnlinkFromAsync(platform, cancellationToken);
        }

        /// 合并为支持 AuthData 的格式
        static void MergeAuthData(IDictionary<string, object> authData, string unionId, AVUserAuthDataLogInOption options) {
            authData["platform"] = options.UnionIdPlatform;
            authData["main_account"] = options.AsMainAccount;
            authData["unionid"] = unionId;
        }
        #endregion
    }
}
