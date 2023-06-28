using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    public class UserHookTest : BaseTest {
        [Test]
        [Order(0)]
        public async Task HookOnLogin() {
            string username = "forbidden";
            string password = "xxxxxx";
            try {
                LCUser user = new LCUser {
                    Username = username,
                    Password = password
                };
                await user.SignUp();
            } catch (LCException e) {
                if (e.Code != 202) {
                    throw e;
                }
            }
            LCException ex = Assert.ThrowsAsync<LCException>(() => LCUser.Login(username, password));
            Assert.AreEqual(ex.Code, 142);
        }

        [Test]
        [Order(1)]
        public async Task HookOnAuthData() {
            string platform = "fake_platform";
            Dictionary<string, object> authData = new Dictionary<string, object> {
                { "openid", "1234" },
                { "access_token", "haha" }
            };
            LCException ex = Assert.ThrowsAsync<LCException>(() => LCUser.LoginWithAuthData(authData, platform));
            Assert.AreEqual(ex.Code, 142);

            authData["openid"] = "123";
            await LCUser.LoginWithAuthData(authData, platform);
        }
    }
}
