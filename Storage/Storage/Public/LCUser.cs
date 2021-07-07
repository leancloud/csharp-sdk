using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Common;
using LeanCloud.Storage.Internal.Object;

namespace LeanCloud.Storage {
    /// <summary>
    /// LCUser represents a user for a LeanCloud application.
    /// </summary>
    public class LCUser : LCObject {
        public const string CLASS_NAME = "_User";

        private const string USER_DATA = ".userdata";
        private const string ANONYMOUS_DATA = ".anonymousdata";

        public string Username {
            get {
                return this["username"] as string;
            } set {
                this["username"] = value;
            }
        }

        public string Password {
            get {
                return this["password"] as string;
            } set {
                this["password"] = value;
            }
        }

        public string Email {
            get {
                return this["email"] as string;
            } set {
                this["email"] = value;
            }
        }

        public string Mobile {
            get {
                return this["mobilePhoneNumber"] as string;
            } set {
                this["mobilePhoneNumber"] = value;
            }
        }

        public string SessionToken {
            get {
                return this["sessionToken"] as string;
            } set {
                this["sessionToken"] = value;
            }
        }

        public bool EmailVerified {
            get {
                return Convert.ToBoolean(this["emailVerified"]);
            }
        }

        public bool MobileVerified {
            get {
                return Convert.ToBoolean(this["mobilePhoneVerified"]);
            }
        }

        public Dictionary<string, object> AuthData {
            get {
                return this["authData"] as Dictionary<string, object>;
            } set {
                this["authData"] = value;
            }
        }

        /// <summary>
        /// Checks whether this user is anonymous.
        /// </summary>
        public bool IsAnonymous => AuthData != null &&
            AuthData.ContainsKey("anonymous");

        static LCUser currentUser;

        /// <summary>
        /// Gets the currently logged in LCUser with a valid session, from
        /// memory or disk if necessary.
        /// </summary>
        /// <returns></returns>
        public static async Task<LCUser> GetCurrent() {
            if (currentUser != null) {
                return currentUser;
            }

            string data = await LCCore.PersistenceController.ReadText(USER_DATA);
            if (!string.IsNullOrEmpty(data)) {
                try {
                    currentUser = ParseObject(data) as LCUser;
                } catch (Exception e) {
                    LCLogger.Error(e);
                    await LCCore.PersistenceController.Delete(USER_DATA);
                }
            }
            return currentUser;
        }

        public LCUser() : base(CLASS_NAME) {
            
        }

        /// <summary>
        /// Signs up a new user.
        /// </summary>
        /// <returns></returns>
        public async Task<LCUser> SignUp() {
            if (string.IsNullOrEmpty(Username)) {
                throw new ArgumentNullException(nameof(Username));
            }
            if (string.IsNullOrEmpty(Password)) {
                throw new ArgumentNullException(nameof(Password));
            }
            if (!string.IsNullOrEmpty(ObjectId)) {
                throw new ArgumentException("Cannot sign up a user that already exists.");
            }
            await Save();

            return this;
        }

        /// <summary>
        /// Requests sending a login sms code.
        /// </summary>
        /// <param name="mobile">The mobile number of an existing user</param>
        /// <returns></returns>
        public static async Task RequestLoginSMSCode(string mobile) {
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(mobile));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile }
            };
            await LCCore.HttpClient.Post<Dictionary<string, object>>("requestLoginSmsCode", data: data);
        }

        /// <summary>
        /// Signs up or signs in a user with their mobile number and verification code.
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static async Task<LCUser> SignUpOrLoginByMobilePhone(string mobile, string code) {
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(mobile));
            }
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(code));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile },
                { "smsCode", code }
            };
            Dictionary<string, object> response = await LCCore.HttpClient.Post<Dictionary<string, object>>("usersByMobilePhone", data: data);
            LCObjectData objectData = LCObjectData.Decode(response);
            currentUser = GenerateUser(objectData);

            await SaveToLocal();

            return currentUser;
        }

        /// <summary>
        /// Signs in a user with their username and password.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static Task<LCUser> Login(string username, string password) {
            if (string.IsNullOrEmpty(username)) {
                throw new ArgumentNullException(nameof(username));
            }
            if (string.IsNullOrEmpty(password)) {
                throw new ArgumentNullException(nameof(password));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "username", username },
                { "password", password }
            };
            return Login(data);
        }

        /// <summary>
        /// Signs in a user with their email and password.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static Task<LCUser> LoginByEmail(string email, string password) {
            if (string.IsNullOrEmpty(email)) {
                throw new ArgumentNullException(nameof(email));
            }
            if (string.IsNullOrEmpty(password)) {
                throw new ArgumentNullException(nameof(password));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "email", email },
                { "password", password }
            };
            return Login(data);
        }

        /// <summary>
        /// Signs in a user with their mobile number and password.
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static Task<LCUser> LoginByMobilePhoneNumber(string mobile, string password) {
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(mobile));
            }
            if (string.IsNullOrEmpty(password)) {
                throw new ArgumentNullException(nameof(password));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile },
                { "password", password }
            };
            return Login(data);
        }

        /// <summary>
        /// Signs in a user with their mobile number and verification code.
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static Task<LCUser> LoginBySMSCode(string mobile, string code) {
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(mobile));
            }
            if (string.IsNullOrEmpty(code)) {
                throw new ArgumentNullException(nameof(code));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile },
                { "smsCode", code }
            };
            return Login(data);
        }

        /// <summary>
        /// Signs up or signs in a user with third party authData.
        /// </summary>
        /// <param name="authData"></param>
        /// <param name="platform"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static Task<LCUser> LoginWithAuthData(Dictionary<string, object> authData, string platform,
            LCUserAuthDataLoginOption option = null) {
            if (authData == null) {
                throw new ArgumentNullException(nameof(authData));
            }
            if (string.IsNullOrEmpty(platform)) {
                throw new ArgumentNullException(nameof(platform));
            }
            if (option == null) {
                option = new LCUserAuthDataLoginOption();
            }
            return LoginWithAuthData(platform, authData, option.FailOnNotExist);
        }

        /// <summary>
        /// Signs up or signs in a user with third party authData and unionId.
        /// </summary>
        /// <param name="authData"></param>
        /// <param name="platform"></param>
        /// <param name="unionId"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static Task<LCUser> LoginWithAuthDataAndUnionId(Dictionary<string, object> authData, string platform, string unionId,
            LCUserAuthDataLoginOption option = null) {
            if (authData == null) {
                throw new ArgumentNullException(nameof(authData));
            }
            if (string.IsNullOrEmpty(platform)) {
                throw new ArgumentNullException(nameof(platform));
            }
            if (string.IsNullOrEmpty(unionId)) {
                throw new ArgumentNullException(nameof(unionId));
            }
            if (option == null) {
                option = new LCUserAuthDataLoginOption();
            }
            MergeAuthData(authData, unionId, option);
            return LoginWithAuthData(platform, authData, option.FailOnNotExist);
        }

        /// <summary>
        /// Associates this user with a third party authData. 
        /// </summary>
        /// <param name="authData"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        public Task AssociateAuthData(Dictionary<string, object> authData, string platform) {
            if (authData == null) {
                throw new ArgumentNullException(nameof(authData));
            }
            if (string.IsNullOrEmpty(platform)) {
                throw new ArgumentNullException(nameof(platform));
            }
            return LinkWithAuthData(platform, authData);
        }

        /// <summary>
        /// Associates this user with a third party authData and unionId.
        /// </summary>
        /// <param name="authData"></param>
        /// <param name="platform"></param>
        /// <param name="unionId"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public Task AssociateAuthDataAndUnionId(Dictionary<string, object> authData, string platform, string unionId,
            LCUserAuthDataLoginOption option = null) {
            if (authData == null) {
                throw new ArgumentNullException(nameof(authData));
            }
            if (string.IsNullOrEmpty(platform)) {
                throw new ArgumentNullException(nameof(platform));
            }
            if (string.IsNullOrEmpty(unionId)) {
                throw new ArgumentNullException(nameof(unionId));
            }
            if (option == null) {
                option = new LCUserAuthDataLoginOption();
            }
            MergeAuthData(authData, unionId, option);
            return LinkWithAuthData(platform, authData);
        }

        /// <summary>
        /// Unlinks a user from a third party platform.
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        public Task DisassociateWithAuthData(string platform) {
            if (string.IsNullOrEmpty(platform)) {
                throw new ArgumentNullException(nameof(platform));
            }
            return UnlinkWithAuthData(platform);
        }

        /// <summary>
        /// Creates an anonymous user.
        /// </summary>
        /// <returns></returns>
        public static async Task<LCUser> LoginAnonymously() {
            string anonymousId = await LCCore.PersistenceController.ReadText(ANONYMOUS_DATA);
            if (string.IsNullOrEmpty(anonymousId)) {
                anonymousId = Guid.NewGuid().ToString();
                await LCCore.PersistenceController.WriteText(ANONYMOUS_DATA, anonymousId);
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "id", anonymousId }
            };
            return await LoginWithAuthData(data, "anonymous");
        }

        /// <summary>
        /// Requests a verification email to be sent to a user's email address.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static async Task RequestEmailVerify(string email) {
            if (string.IsNullOrEmpty(email)) {
                throw new ArgumentNullException(nameof(email));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "email", email }
            };
            await LCCore.HttpClient.Post<Dictionary<string, object>>("requestEmailVerify", data: data);
        }

        /// <summary>
        /// Requests a verification SMS to be sent to a user's mobile number.
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static async Task RequestMobilePhoneVerify(string mobile) {
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(mobile));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile }
            };
            await LCCore.HttpClient.Post<Dictionary<string, object>>("requestMobilePhoneVerify", data: data);
        }

        /// <summary>
        /// Requests to verify a user's mobile number with sms code they received.
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static async Task VerifyMobilePhone(string mobile, string code) {
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(mobile));
            }
            if (string.IsNullOrEmpty(code)) {
                throw new ArgumentNullException(nameof(code));
            }
            string path = $"verifyMobilePhone/{code}";
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile }
            };
            await LCCore.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }

        /// <summary>
        /// Signs in a user with a sessionToken.
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <returns></returns>
        public static async Task<LCUser> BecomeWithSessionToken(string sessionToken) {
            if (string.IsNullOrEmpty(sessionToken)) {
                throw new ArgumentNullException(nameof(sessionToken));
            }
            Dictionary<string, object> headers = new Dictionary<string, object> {
                { "X-LC-Session", sessionToken }
            };
            Dictionary<string, object> response = await LCCore.HttpClient.Get<Dictionary<string, object>>("users/me",
                headers: headers);
            LCObjectData objectData = LCObjectData.Decode(response);
            currentUser = GenerateUser(objectData);
            return currentUser;
        }

        /// <summary>
        /// Requests a password reset email to be sent to a user's email address.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static async Task RequestPasswordReset(string email) {
            if (string.IsNullOrEmpty(email)) {
                throw new ArgumentNullException(nameof(email));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "email", email }
            };
            await LCCore.HttpClient.Post<Dictionary<string, object>>("requestPasswordReset",
                data: data);
        }

        /// <summary>
        /// Requests a reset password sms code to be sent to a user's mobile number.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static async Task RequestPasswordResetBySmsCode(string mobile) {
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(mobile));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile }
            };
            await LCCore.HttpClient.Post<Dictionary<string, object>>("requestPasswordResetBySmsCode",
                data: data);
        }

        /// <summary>
        /// Resets a user's password via mobile phone.
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="code"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public static async Task ResetPasswordBySmsCode(string mobile, string code, string newPassword) {
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(mobile));
            }
            if (string.IsNullOrEmpty(code)) {
                throw new ArgumentNullException(nameof(code));
            }
            if (string.IsNullOrEmpty(newPassword)) {
                throw new ArgumentNullException(nameof(newPassword));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile },
                { "password", newPassword }
            };
            await LCCore.HttpClient.Put<Dictionary<string, object>>($"resetPasswordBySmsCode/{code}",
                data: data);
        }

        /// <summary>
        /// Updates newPassword safely with oldPassword.
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public async Task UpdatePassword(string oldPassword, string newPassword) {
            if (string.IsNullOrEmpty(oldPassword)) {
                throw new ArgumentNullException(nameof(oldPassword));
            }
            if (string.IsNullOrEmpty(newPassword)) {
                throw new ArgumentNullException(nameof(newPassword));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "old_password", oldPassword },
                { "new_password", newPassword }
            };
            Dictionary<string, object> response = await LCCore.HttpClient.Put<Dictionary<string, object>>(
                $"users/{ObjectId}/updatePassword", data:data);
            LCObjectData objectData = LCObjectData.Decode(response);
            Merge(objectData);
        }

        /// <summary>
        /// Logs out the currently logged in user session.
        /// </summary>
        public static Task Logout() {
            currentUser = null;
            // 清理持久化数据
            return LCCore.PersistenceController.Delete(USER_DATA);
        }

        /// <summary>
        /// Checks whether the current sessionToken is valid.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsAuthenticated() {
            if (SessionToken == null || ObjectId == null) {
                return false;
            }
            try {
                await LCCore.HttpClient.Get<Dictionary<string, object>>("users/me");
                return true;
            } catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Constructs a LCQuery for LCUser.
        /// </summary>
        /// <returns></returns>
        public static LCQuery<LCUser> GetQuery() {
            return new LCQuery<LCUser>(CLASS_NAME);
        }

        async Task LinkWithAuthData(string authType, Dictionary<string, object> data) {
            Dictionary<string, object> oriAuthData = new Dictionary<string, object>(AuthData ?? new Dictionary<string, object>());
            AuthData = new Dictionary<string, object> {
                { authType, data }
            };
            try {
                await base.Save();
                oriAuthData[authType] = data;
                await UpdateAuthData(oriAuthData);
            } catch (Exception e) {
                AuthData = oriAuthData;
                throw e;
            }
        }

        async Task UnlinkWithAuthData(string authType) {
            Dictionary<string, object> oriAuthData = new Dictionary<string, object>(AuthData);
            AuthData = new Dictionary<string, object> {
                { authType, null }
            };
            try {
                await base.Save();
                oriAuthData.Remove(authType);
                await UpdateAuthData(oriAuthData);
            } catch (Exception e) {
                AuthData = oriAuthData;
                throw e;
            }
        }

        private async Task UpdateAuthData(Dictionary<string, object> authData) {
            LCObjectData objData = new LCObjectData();
            objData.CustomPropertyDict["authData"] = authData;
            Merge(objData);
            await SaveToLocal();
        }

        static async Task<LCUser> Login(Dictionary<string, object> data) {
            Dictionary<string, object> response = await LCCore.HttpClient.Post<Dictionary<string, object>>("login", data: data);
            LCObjectData objectData = LCObjectData.Decode(response);
            currentUser = GenerateUser(objectData);

            await SaveToLocal();

            return currentUser;
        }

        static async Task<LCUser> LoginWithAuthData(string authType, Dictionary<string, object> data, bool failOnNotExist) {
            Dictionary<string, object> authData = new Dictionary<string, object> {
                { authType, data }
            };
            string path = failOnNotExist ? "users?failOnNotExist=true" : "users";
            Dictionary<string, object> response = await LCCore.HttpClient.Post<Dictionary<string, object>>(path, data: new Dictionary<string, object> {
                { "authData", authData }
            });
            LCObjectData objectData = LCObjectData.Decode(response);

            currentUser = GenerateUser(objectData);

            await SaveToLocal();

            return currentUser;
        }

        static void MergeAuthData(Dictionary<string, object> authData, string unionId, LCUserAuthDataLoginOption option) {
            authData["platform"] = option.UnionIdPlatform;
            authData["main_account"] = option.AsMainAccount;
            authData["unionid"] = unionId;
        }

        private static async Task SaveToLocal() {
            try {
                string json = currentUser.ToString();
                await LCCore.PersistenceController.WriteText(USER_DATA, json);
            } catch (Exception e) {
                LCLogger.Error(e.Message);
            }
        }

        /// <summary>
        /// Requests an SMS code for updating phone number.
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="ttl"></param>
        /// <param name="captchaToken"></param>
        /// <returns></returns>
        public static async Task RequestSMSCodeForUpdatingPhoneNumber(string mobile, int ttl = 360, string captchaToken = null) {
            string path = "requestChangePhoneNumber";
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile },
                { "ttl", ttl }
            };
            if (!string.IsNullOrEmpty(captchaToken)) {
                data["validate_token"] = captchaToken;
            }
            await LCCore.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }

        /// <summary>
        /// Verify code for updating phone number.
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static async Task VerifyCodeForUpdatingPhoneNumber(string mobile, string code) {
            string path = "changePhoneNumber";
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile },
                { "code", code }
            };
            await LCCore.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }

        /// <summary>
        /// Follows a user.
        /// </summary>
        /// <param name="targetId"></param>
        /// <param name="attrs"></param>
        /// <returns></returns>
        public async Task Follow(string targetId, Dictionary<string, object> attrs = null) {
            if (string.IsNullOrEmpty(targetId)) {
                throw new ArgumentNullException(nameof(targetId));
            }
            string path = $"users/self/friendship/{targetId}";
            await LCCore.HttpClient.Post<Dictionary<string, object>>(path, data: attrs);
        }

        /// <summary>
        /// Unfollows a user.
        /// </summary>
        /// <param name="targetId"></param>
        /// <returns></returns>
        public async Task Unfollow(string targetId) {
            if (string.IsNullOrEmpty(targetId)) {
                throw new ArgumentNullException(nameof(targetId));
            }
            string path = $"users/self/friendship/{targetId}";
            await LCCore.HttpClient.Delete(path);
        }

        /// <summary>
        /// Constructs a follower query.
        /// </summary>
        /// <returns></returns>
        public LCQuery<LCObject> FollowerQuery() {
            return new LCQuery<LCObject>("_Follower")
                .WhereEqualTo("user", this)
                .Include("follower");
        }

        /// <summary>
        /// Constructs a followee query.
        /// </summary>
        /// <returns></returns>
        public LCQuery<LCObject> FolloweeQuery() {
            return new LCQuery<LCObject>("_Followee")
                .WhereEqualTo("user", this)
                .Include("followee");
        }

        /// <summary>
        /// Gets the followers and followees of the currently logged in user.
        /// </summary>
        /// <param name="includeFollower"></param>
        /// <param name="includeFollowee"></param>
        /// <param name="returnCount"></param>
        /// <returns></returns>
        public async Task<LCFollowersAndFollowees> GetFollowersAndFollowees(bool includeFollower = false,
            bool includeFollowee = false, bool returnCount = false) {
            Dictionary<string, object> queryParams = new Dictionary<string, object>();
            if (returnCount) {
                queryParams["count"] = 1;
            }
            if (includeFollower || includeFollowee) {
                List<string> includes = new List<string>();
                if (includeFollower) {
                    includes.Add("follower");
                }
                if (includeFollowee) {
                    includes.Add("followee");
                }
                queryParams["include"] = string.Join(",", includes);
            }
            string path = $"users/{ObjectId}/followersAndFollowees";
            Dictionary<string, object> response = await LCCore.HttpClient.Get<Dictionary<string, object>>(path,
                queryParams: queryParams);
            LCFollowersAndFollowees result = new LCFollowersAndFollowees();
            if (response.TryGetValue("followers", out object followersObj) &&
                (followersObj is List<object> followers)) {
                result.Followers = new List<LCObject>();
                foreach (object followerObj in followers) {
                    LCObjectData objectData = LCObjectData.Decode(followerObj as IDictionary);
                    LCObject follower = new LCObject("_Follower");
                    follower.Merge(objectData);
                    result.Followers.Add(follower);
                }
            }
            if (response.TryGetValue("followees", out object followeesObj) &&
                (followeesObj is List<object> followees)) {
                result.Followees = new List<LCObject>();
                foreach (object followeeObj in followees) {
                    LCObjectData objectData = LCObjectData.Decode(followeeObj as IDictionary);
                    LCObject followee = new LCObject("_Followee");
                    followee.Merge(objectData);
                    result.Followees.Add(followee);
                }
            }
            if (response.TryGetValue("followers_count", out object followersCountObj) &&
                (followersCountObj is int followersCount)) {
                result.FollowersCount = followersCount;
            }
            if (response.TryGetValue("followees_count", out object followeesCountObj) &&
                (followeesCountObj is int followeesCount)) {
                result.FolloweesCount = followeesCount;
            }
            return result;
        }

        public new async Task<LCUser> Save(bool fetchWhenSave = false, LCQuery<LCObject> query = null) {
            await base.Save(fetchWhenSave, query);
            currentUser = this;
            await SaveToLocal();
            return this;
        }

        public static LCUser GenerateUser(LCObjectData objectData) {
            LCUser user = Create(CLASS_NAME) as LCUser;
            user.Merge(objectData);
            return user;
        }
    }
}
