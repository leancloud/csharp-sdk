using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Storage;
using Newtonsoft.Json;

namespace Storage.Test {
    public class UserTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Print;
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Print;
        }

        [Test]
        public async Task SignUp() {
            LCUser user = new LCUser();
            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            user.Username = $"{unixTime}";
            user.Password = "world";
            string email = $"{unixTime}@qq.com";
            user.Email = email;
            Random random = new Random();
            string mobile = $"151{random.Next(10000000, 99999999)}";
            user.Mobile = mobile;
            await user.SignUp();

            TestContext.WriteLine(user.Username);
            TestContext.WriteLine(user.Password);

            Assert.NotNull(user.ObjectId);
            TestContext.WriteLine(user.ObjectId);
            Assert.NotNull(user.SessionToken);
            TestContext.WriteLine(user.SessionToken);
            Assert.AreEqual(user.Email, email);
        }

        [Test]
        public async Task Login() {
            await LCUser.Login("hello", "world");
            LCUser current = await LCUser.GetCurrent();
            Assert.NotNull(current.ObjectId);
            Assert.IsFalse(current.EmailVerified);
            Assert.IsFalse(current.MobileVerified);
            Assert.AreEqual(current.Mobile, "15101006008");
        }

        [Test]
        public async Task LoginByEmail() {
            await LCUser.LoginByEmail("171253484@qq.com", "world");
            LCUser current = await LCUser.GetCurrent();
            Assert.NotNull(current.ObjectId);
        }

        [Test]
        public async Task LoginBySessionToken() {
            await LCUser.Logout();
            string sessionToken = "luo2fpl4qij2050e7enqfz173";
            await LCUser.BecomeWithSessionToken(sessionToken);
            LCUser current = await LCUser.GetCurrent();
            Assert.NotNull(current.ObjectId);
        }

        [Test]
        public async Task RelateObject() {
            LCUser user = await LCUser.LoginByMobilePhoneNumber("15101006007", "112358");
            LCObject account = new LCObject("Account");
            account["user"] = user;
            await account.Save();
        }

        [Test]
        public async Task LoginAnonymous() {
            LCUser user = await LCUser.LoginAnonymously();
            Assert.NotNull(user.ObjectId);
            Assert.IsTrue(user.IsAnonymous);
        }

        [Test]
        public async Task LoginWithAuthData() {
            string uuid = Guid.NewGuid().ToString();
            Dictionary<string, object> authData = new Dictionary<string, object> {
                { "expires_in", 7200 },
                { "openid", uuid },
                { "access_token", uuid }
            };
            LCUser currentUser = await LCUser.LoginWithAuthData(authData, "weixin");
            TestContext.WriteLine(currentUser.SessionToken);
            Assert.NotNull(currentUser.SessionToken);
            string userId = currentUser.ObjectId;
            TestContext.WriteLine($"userId: {userId}");
            TestContext.WriteLine(currentUser.AuthData);

            await LCUser.Logout();
            currentUser = await LCUser.GetCurrent();
            Assert.IsNull(currentUser);

            currentUser = await LCUser.LoginWithAuthData(authData, "weixin");
            TestContext.WriteLine(currentUser.SessionToken);
            Assert.NotNull(currentUser.SessionToken);
            Assert.AreEqual(currentUser.ObjectId, userId);
            TestContext.WriteLine(currentUser.AuthData);
        }

        [Test]
        public async Task AssociateAuthData() {
            string uuid = Guid.NewGuid().ToString();
            LCUser currentUser = await LCUser.Login("hello", "world");
            Dictionary<string, object> authData = new Dictionary<string, object> {
                { "expires_in", 7200 },
                { "openid", uuid },
                { "access_token", uuid }
            };
            await currentUser.AssociateAuthData(authData, "weixin");
            TestContext.WriteLine(currentUser.AuthData);
            TestContext.WriteLine(currentUser.AuthData["weixin"]);
        }

        [Test]
        public async Task DisassociateAuthData() {
            LCUser currentUser = await LCUser.Login("hello", "world");
            await currentUser.DisassociateWithAuthData("weixin");
        }

        [Test]
        public async Task IsAuthenticated() {
            LCUser currentUser = await LCUser.Login("hello", "world");
            bool isAuthenticated = await currentUser.IsAuthenticated();
            TestContext.WriteLine(isAuthenticated);
            Assert.IsTrue(isAuthenticated);
        }

        [Test]
        public async Task UpdatePassword() {
            LCUser currentUser = await LCUser.Login("hello", "world");
            await currentUser.UpdatePassword("world", "newWorld");
            await currentUser.UpdatePassword("newWorld", "world");
        }

        [Test]
        public async Task LoginWithAuthDataWithUnionId() {
            string uuid = Guid.NewGuid().ToString();
            Dictionary<string, object> authData = new Dictionary<string, object> {
                { "expires_in", 7200 },
                { "openid", uuid },
                { "access_token", uuid }
            };
            string unionId = Guid.NewGuid().ToString();

            LCUserAuthDataLoginOption option = new LCUserAuthDataLoginOption();
            option.AsMainAccount = true;
            LCUser currentUser = await LCUser.LoginWithAuthDataAndUnionId(authData, "weixin_app", unionId, option: option);
            TestContext.WriteLine(currentUser.SessionToken);
            Assert.NotNull(currentUser.SessionToken);
            string userId = currentUser.ObjectId;
            TestContext.WriteLine($"userId: {userId}");
            TestContext.WriteLine(currentUser.AuthData);

            await LCUser.Logout();
            currentUser = await LCUser.GetCurrent();
            Assert.IsNull(currentUser);

            currentUser = await LCUser.LoginWithAuthDataAndUnionId(authData, "weixin_mini_app", unionId, option: option);
            TestContext.WriteLine(currentUser.SessionToken);
            Assert.NotNull(currentUser.SessionToken);
            Assert.AreEqual(currentUser.ObjectId, userId);
            TestContext.WriteLine(currentUser.AuthData);
        }

        [Test]
        public async Task AssociateAuthDataWithUnionId() {
            LCUser currentUser = await LCUser.Login("hello", "world");
            string uuid = Guid.NewGuid().ToString();
            Dictionary<string, object> authData = new Dictionary<string, object> {
                { "expires_in", 7200 },
                { "openid", uuid },
                { "access_token", uuid }
            };
            string unionId = Guid.NewGuid().ToString();
            await currentUser.AssociateAuthDataAndUnionId(authData, "qq", unionId);
        }

        // 手动测试

        //[Test]
        //public async Task LoginByMobile() {
        //    LCUser user = await LCUser.LoginByMobilePhoneNumber("15101006007", "112358");
        //    Assert.NotNull(user.ObjectId);
        //}

        //[Test]
        //public async Task RequestLoginSMSCode() {
        //    await LCUser.RequestLoginSMSCode("15101006007");
        //}

        //[Test]
        //public async Task LoginBySMSCode() {
        //    LCUser user = await LCUser.LoginBySMSCode("15101006007", "194998");
        //    Assert.NotNull(user.ObjectId);
        //}

        //[Test]
        //public async Task RequestEmailVerify() {
        //    await LCUser.RequestEmailVerify("171253484@qq.com");
        //}

        //[Test]
        //public async Task RequestMobileVerify() {
        //    await LCUser.RequestMobilePhoneVerify("15101006007");
        //}

        //[Test]
        //public async Task VerifyMobile() {
        //    await LCUser.VerifyMobilePhone("15101006007", "506993");
        //}

        //[Test]
        //public async Task RequestResetPasswordBySMSCode() {
        //    await LCUser.RequestPasswordRestBySmsCode("15101006007");
        //}

        //[Test]
        //public async Task ResetPasswordBySMSCode() {
        //    await LCUser.ResetPasswordBySmsCode("15101006007", "732552", "112358");
        //}

        [Test]
        public async Task RequestSMSCodeForUpdatingPhoneNumber() {
            await LCUser.Login("hello", "world");
            await LCUser.RequestSMSCodeForUpdatingPhoneNumber("15101006007");
        }

        [Test]
        public async Task VerifyCodeForUpdatingPhoneNumber() {
            await LCUser.Login("hello", "world");
            await LCUser.VerifyCodeForUpdatingPhoneNumber("15101006007", "969327");
        }

        [Test]
        public async Task AuthData() {
            string uuid = Guid.NewGuid().ToString();
            Dictionary<string, object> authData = new Dictionary<string, object> {
                { "expires_in", 7200 },
                { "openid", uuid },
                { "access_token", uuid }
            };
            LCUser currentUser = await LCUser.LoginWithAuthData(authData, "weixin");
            TestContext.WriteLine(currentUser.SessionToken);
            Assert.NotNull(currentUser.SessionToken);
            string userId = currentUser.ObjectId;
            TestContext.WriteLine($"userId: {userId}");
            TestContext.WriteLine(JsonConvert.SerializeObject(currentUser.AuthData));

            try {
                authData = new Dictionary<string, object> {
                    { "expires_in", 7200 },
                    { "openid", uuid },
                    { "access_token", uuid }
                };
                await currentUser.AssociateAuthData(authData, "qq");
                TestContext.WriteLine(JsonConvert.SerializeObject(currentUser.AuthData));
            } catch (LCException e) {
                TestContext.WriteLine($"{e.Code} : {e.Message}");
                TestContext.WriteLine(JsonConvert.SerializeObject(currentUser.AuthData));
            }
        }
    }
}
