using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Storage;

namespace Storage.Test {
    public class SMSTest : BaseTest {
        //[Test]
        public async Task RequestSMS() {
            await LCSMSClient.RequestSMSCode(TestPhone,
                template: "test_template",
                signature: "flutter-test",
                variables: new Dictionary<string, object> {
                    { "k1", "v1" }
                });
        }

        //[Test]
        public async Task RequestVoice() {
            await LCSMSClient.RequestVoiceCode(TestPhone);
        }

        [Test]
        public async Task Verify() {
            await LCSMSClient.VerifyMobilePhone(TestPhone, TestSMSCode);
        }
    }
}
