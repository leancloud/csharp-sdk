using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud;
using LeanCloud.Common;
using LeanCloud.Storage;
using LeanCloud.Realtime;

using static System.Console;

namespace Realtime.Test {
    public class Message {
        private LCIMClient m1;
        private LCIMClient m2;

        private LCIMConversation conversation;

        [SetUp]
        public async Task SetUp() {
            LCLogger.LogDelegate += Utils.Print;
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
            m1 = new LCIMClient("m1");
            m2 = new LCIMClient("m2");
            await m1.Open();
            await m2.Open();
            conversation = await m1.CreateConversation(new string[] { "m2" });
        }

        [TearDown]
        public async Task TearDown() {
            await m1.Close();
            await m2.Close();
            LCLogger.LogDelegate -= Utils.Print;
        }

        [Test]
        public async Task Send() {
            AutoResetEvent are = new AutoResetEvent(false);
            m2.OnMessage = (conv, msg) => {
                WriteLine(msg.Id);
                if (msg is LCIMImageMessage imageMsg) {
                    WriteLine($"-------- url: {imageMsg.Url}");
                } else if (msg is LCIMFileMessage fileMsg) {
                    WriteLine($"-------- name: {fileMsg.Format}");
                } else if (msg is LCIMTextMessage textMsg) {
                    WriteLine($"-------- text: {textMsg.Text}");
                }
            };

            LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
            await conversation.Send(textMessage);
            Assert.NotNull(textMessage.Id);

            LCFile image = new LCFile("hello", "../../../../assets/hello.png");
            await image.Save();
            LCIMImageMessage imageMessage = new LCIMImageMessage(image);
            await conversation.Send(imageMessage);
            Assert.NotNull(imageMessage.Id);

            LCFile file = new LCFile("apk", "../../../../assets/test.apk");
            await file.Save();
            LCIMFileMessage fileMessage = new LCIMFileMessage(file);
            await conversation.Send(fileMessage);
            Assert.NotNull(fileMessage.Id);
        }
    }
}
