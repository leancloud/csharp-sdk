﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Storage;
using LC.Newtonsoft.Json;

namespace Storage.Test {
    public class UserTest : BaseTest {
        [Test]
        [Order(0)]
        public async Task SignUp() {
            LCUser user = new LCUser();
            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            user.Username = $"{unixTime}";
            user.Password = "world";
            string email = $"{unixTime}@qq.com";
            user.Email = email;
            //Random random = new Random();
            //string mobile = $"151{random.Next(10000000, 99999999)}";
            //user.Mobile = mobile;
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
        [Order(1)]
        public async Task Login() {
            try {
                await LCUser.Login(TestPhone, TestPhone);
            } catch (LCException e) {
                if (e.Code == 211) {
                    LCUser user = new LCUser {
                        Username = TestPhone,
                        Password = TestPhone,
                        Mobile = TestPhone,
                        Email = GetTestEmail()
                    };
                    await user.SignUp();
                } else {
                    throw e;
                }
            }

            await LCUser.Login(TestPhone, TestPhone);
            LCUser current = await LCUser.GetCurrent();
            Assert.NotNull(current.ObjectId);
            Assert.IsFalse(current.EmailVerified);
            Assert.IsTrue(current.MobileVerified);
            Assert.AreEqual(current.Mobile, TestPhone);
        }

        [Test]
        [Order(2)]
        public async Task LoginByEmail() {
            await LCUser.LoginByEmail(GetTestEmail(), TestPhone);
            LCUser current = await LCUser.GetCurrent();
            Assert.NotNull(current.ObjectId);
        }

        [Test]
        [Order(3)]
        public async Task LoginBySessionToken() {
            LCUser user = await LCUser.Login(TestPhone, TestPhone);
            string sessionToken = user.SessionToken;
            await LCUser.Logout();

            await LCUser.BecomeWithSessionToken(sessionToken);
            LCUser current = await LCUser.GetCurrent();
            Assert.NotNull(current.ObjectId);
        }

        [Test]
        [Order(4)]
        public async Task RelateObject() {
            LCUser user = await LCUser.LoginByMobilePhoneNumber(TestPhone, TestPhone);
            Account account = new Account();
            account.User = user;
            await account.Save();
        }

        [Test]
        [Order(5)]
        public async Task LoginAnonymous() {
            LCUser user = await LCUser.LoginAnonymously();
            Assert.NotNull(user.ObjectId);
            Assert.IsTrue(user.IsAnonymous);
        }

        [Test]
        [Order(6)]
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
        [Order(7)]
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
        [Order(8)]
        public async Task DisassociateAuthData() {
            LCUser currentUser = await LCUser.Login("hello", "world");
            await currentUser.DisassociateWithAuthData("weixin");
        }

        [Test]
        [Order(9)]
        public async Task IsAuthenticated() {
            LCUser currentUser = await LCUser.Login("hello", "world");
            bool isAuthenticated = await currentUser.IsAuthenticated();
            TestContext.WriteLine(isAuthenticated);
            Assert.IsTrue(isAuthenticated);
        }

        [Test]
        [Order(10)]
        public async Task UpdatePassword() {
            LCUser currentUser = await LCUser.Login("hello", "world");
            await currentUser.UpdatePassword("world", "newWorld");
            await currentUser.UpdatePassword("newWorld", "world");
        }

        [Test]
        [Order(11)]
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
        [Order(12)]
        public async Task AssociateAuthDataWithUnionId() {
            LCUser currentUser = await LCUser.Login(TestPhone, TestPhone);
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

        [Test]
        [Order(13)]
        public async Task LoginByMobile() {
            LCUser user = await LCUser.LoginByMobilePhoneNumber(TestPhone, TestPhone);
            Assert.NotNull(user.ObjectId);
        }

        //[Test]
        //public async Task RequestLoginSMSCode() {
        //    await LCUser.RequestLoginSMSCode("15101006007");
        //}

        [Test]
        [Order(14)]
        public async Task LoginBySMSCode() {
            LCUser user = await LCUser.LoginBySMSCode(TestPhone, TestSMSCode);
            Assert.NotNull(user.ObjectId);
        }

        //[Test]
        //public async Task RequestEmailVerify() {
        //    await LCUser.RequestEmailVerify("171253484@qq.com");
        //}

        //[Test]
        //public async Task RequestMobileVerify() {
        //    await LCUser.RequestMobilePhoneVerify("15101006007");
        //}

        [Test]
        [Order(15)]
        public async Task VerifyMobile() {
            await LCUser.VerifyMobilePhone(TestPhone, TestSMSCode);
        }

        //[Test]
        //public async Task RequestResetPasswordBySMSCode() {
        //    await LCUser.RequestPasswordRestBySmsCode("15101006007");
        //}

        //[Test]
        //public async Task ResetPasswordBySMSCode() {
        //    await LCUser.ResetPasswordBySmsCode("15101006007", "732552", "112358");
        //}

        //[Test]
        //public async Task RequestSMSCodeForUpdatingPhoneNumber() {
        //    await LCUser.Login(TestPhone, TestPhone);
        //    await LCUser.RequestSMSCodeForUpdatingPhoneNumber(TestPhone);
        //}

        [Test]
        [Order(16)]
        public async Task VerifyCodeForUpdatingPhoneNumber() {
            await LCUser.Login(TestPhone, TestPhone);
            await LCUser.VerifyCodeForUpdatingPhoneNumber(TestPhone, TestSMSCode);
        }

        [Test]
        [Order(17)]
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

        private string GetTestEmail() {
            return $"{TestPhone}@leancloud.rocks";
        }
    }
}
