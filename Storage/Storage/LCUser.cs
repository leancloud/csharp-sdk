using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Object;

namespace LeanCloud.Storage {
    public class LCUser : LCObject {
        public const string CLASS_NAME = "_User";

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

        public static Task<LCUser> GetCurrent() {
            // TODO 加载持久化数据

            return Task.FromResult(currentUser);
        }

        public LCUser() : base(CLASS_NAME) {
            
        }

        public LCUser(LCObjectData objectData) : this() {
            Merge(objectData);
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
            currentUser = this;
            // TODO Persistence

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
            await LCApplication.HttpClient.Post<Dictionary<string, object>>("requestLoginSmsCode", data: data);
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
            Dictionary<string, object> response = await LCApplication.HttpClient.Post<Dictionary<string, object>>("usersByMobilePhone", data: data);
            LCObjectData objectData = LCObjectData.Decode(response);
            currentUser = new LCUser(objectData);
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
            return LinkWithAuthData(platform, null);
        }

        /// <summary>
        /// Creates an anonymous user.
        /// </summary>
        /// <returns></returns>
        public static Task<LCUser> LoginAnonymously() {
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "id", Guid.NewGuid().ToString() }
            };
            return LoginWithAuthData(data, "anonymous");
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
            await LCApplication.HttpClient.Post<Dictionary<string, object>>("requestEmailVerify", data: data);
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
            await LCApplication.HttpClient.Post<Dictionary<string, object>>("requestMobilePhoneVerify", data: data);
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
            await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: data);
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
            Dictionary<string, object> response = await LCApplication.HttpClient.Get<Dictionary<string, object>>("users/me",
                headers: headers);
            LCObjectData objectData = LCObjectData.Decode(response);
            currentUser = new LCUser(objectData);
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
            await LCApplication.HttpClient.Post<Dictionary<string, object>>("requestPasswordReset",
                data: data);
        }

        /// <summary>
        /// Requests a reset password sms code to be sent to a user's mobile number.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static async Task RequestPasswordRestBySmsCode(string mobile) {
            if (string.IsNullOrEmpty(mobile)) {
                throw new ArgumentNullException(nameof(mobile));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "mobilePhoneNumber", mobile }
            };
            await LCApplication.HttpClient.Post<Dictionary<string, object>>("requestPasswordResetBySmsCode",
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
            await LCApplication.HttpClient.Put<Dictionary<string, object>>($"resetPasswordBySmsCode/{code}",
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
            Dictionary<string, object> response = await LCApplication.HttpClient.Put<Dictionary<string, object>>(
                $"users/{ObjectId}/updatePassword", data:data);
            LCObjectData objectData = LCObjectData.Decode(response);
            Merge(objectData);
        }

        /// <summary>
        /// Logs out the currently logged in user session.
        /// </summary>
        public static Task Logout() {
            currentUser = null;
            // TODO 清理持久化数据

            return Task.FromResult<object>(null);
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
                await LCApplication.HttpClient.Get<Dictionary<string, object>>("users/me");
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

        Task LinkWithAuthData(string authType, Dictionary<string, object> data) {
            AuthData = new Dictionary<string, object> {
                { authType, data }
            };
            return Save();
        }

        static async Task<LCUser> Login(Dictionary<string, object> data) {
            Dictionary<string, object> response = await LCApplication.HttpClient.Post<Dictionary<string, object>>("login", data: data);
            LCObjectData objectData = LCObjectData.Decode(response);
            currentUser = new LCUser(objectData);
            return currentUser;
        }

        static async Task<LCUser> LoginWithAuthData(string authType, Dictionary<string, object> data, bool failOnNotExist) {
            Dictionary<string, object> authData = new Dictionary<string, object> {
                { authType, data }
            };
            string path = failOnNotExist ? "users?failOnNotExist=true" : "users";
            Dictionary<string, object> response = await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: new Dictionary<string, object> {
                { "authData", authData }
            });
            LCObjectData objectData = LCObjectData.Decode(response);
            currentUser = new LCUser(objectData);
            return currentUser;
        }

        static void MergeAuthData(Dictionary<string, object> authData, string unionId, LCUserAuthDataLoginOption option) {
            authData["platform"] = option.UnionIdPlatform;
            authData["main_account"] = option.AsMainAccount;
            authData["unionid"] = unionId;
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
            await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: data);
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
            await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }
    }
}
