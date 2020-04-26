using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Common;
using LeanCloud.Storage;
using LeanCloud.Realtime;

using static NUnit.Framework.TestContext;

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
        [Order(0)]
        public async Task Send() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            int count = 0;
            m2.OnMessage = (conv, msg) => {
                WriteLine(msg.Id);
                if (msg is LCIMImageMessage imageMsg) {
                    WriteLine($"-------- url: {imageMsg.Url}");
                    count++;
                } else if (msg is LCIMFileMessage fileMsg) {
                    WriteLine($"-------- name: {fileMsg.Format}");
                    count++;
                } else if (msg is LCIMTextMessage textMsg) {
                    WriteLine($"-------- text: {textMsg.Text}");
                    count++;
                }
                if (count >= 3) {
                    tcs.SetResult(null);
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

            await tcs.Task;
        }

        [Test]
        [Order(1)]
        public async Task AckAndRead() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            m2.OnMessage = async (conv, msg) => {
                await conv.Read();
            };
            m1.OnMessageDelivered = (conv, msgId) => {
                WriteLine($"{msgId} is delivered.");
            };
            m1.OnMessageRead = (conv, msgId) => {
                WriteLine($"{msgId} is read.");
                tcs.SetResult(null);
            };
            LCIMTextMessage textMessage = new LCIMTextMessage("hello");
            LCIMMessageSendOptions options = new LCIMMessageSendOptions {
                Receipt = true
            };
            await conversation.Send(textMessage, options);

            await tcs.Task;
        }

        [Test]
        [Order(2)]
        public async Task Recall() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            m2.OnMessageRecalled = (conv, msgId) => {
                WriteLine($"{msgId} is recalled.");
                tcs.SetResult(null);
            };
            LCIMTextMessage textMessage = new LCIMTextMessage("I will be recalled.");
            await conversation.Send(textMessage);
            await Task.Delay(1000);
            await conversation.RecallMessage(textMessage);
            
            await tcs.Task;
        }

        [Test]
        [Order(3)]
        public async Task Update() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            m2.OnMessageUpdated = (conv, msg) => {
                Assert.True(msg is LCIMTextMessage);
                LCIMTextMessage textMessage = msg as LCIMTextMessage;
                Assert.AreEqual(textMessage.Text, "world");
                WriteLine($"{msg.Id} is updated");
                tcs.SetResult(null);
            };
            LCIMTextMessage oldMessage = new LCIMTextMessage("hello");
            await conversation.Send(oldMessage);
            await Task.Delay(1000);
            LCIMTextMessage newMessage = new LCIMTextMessage("world");
            await conversation.UpdateMessage(oldMessage, newMessage);

            await tcs.Task;
        }

        [Test]
        [Order(4)]
        public async Task Query() {
            ReadOnlyCollection<LCIMMessage> messages = await conversation.QueryMessages();
            Assert.Greater(messages.Count, 0);
            foreach (LCIMMessage message in messages) {
                Assert.AreEqual(message.ConversationId, conversation.Id);
                Assert.NotNull(message.Id);
                WriteLine(message.Id);
            }
        }
    }
}
