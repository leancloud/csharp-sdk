using NUnit.Framework;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Storage;

using static NUnit.Framework.TestContext;

namespace Storage.Test {
    public class CaptchaTest {
        [SetUp]
        public void SetUp() {
            Utils.SetUp();
        }

        [TearDown]
        public void TearDown() {
            Utils.TearDown();
        }

        //[Test]
        public async Task Request() {
            LCCapture captcha = await LCCaptchaClient.RequestCaptcha();
            WriteLine($"url: {captcha.Url}");
            WriteLine($"token: {captcha.Token}");
            Assert.NotNull(captcha);
            Assert.NotNull(captcha.Url);
            Assert.NotNull(captcha.Token);
        }

        //[Test]
        public async Task Verify() {
            await LCCaptchaClient.VerifyCaptcha("on2r", "1TUDkEMu");
        }
    }
}
