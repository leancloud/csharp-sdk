using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud;
using LeanCloud.Storage;
using LeanCloud.Realtime;

using static NUnit.Framework.TestContext;

class EmojiMessage : LCIMTypedMessage {
    public const int EmojiMessageType = 1;

    public override int MessageType => EmojiMessageType;

    public string Ecode {
        get {
            return data["ecode"] as string;
        } set {
            data["ecode"] = value;
        }
    }
}

namespace Realtime.Test {
    public class Message {
        private LCIMClient m1;
        private LCIMClient m2;

        private LCIMConversation conversation;

        [SetUp]
        public async Task SetUp() {
            Utils.SetUp();
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
            Utils.TearDown();
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

            LCFile image = new LCFile("hello", "../../../../../assets/hello.png");
            LCIMImageMessage imageMessage = new LCIMImageMessage(image);
            await conversation.Send(imageMessage);
            Assert.NotNull(imageMessage.Id);

            LCFile file = new LCFile("apk", "../../../../../assets/test.apk");
            LCIMFileMessage fileMessage = new LCIMFileMessage(file);
            await conversation.Send(fileMessage);
            Assert.NotNull(fileMessage.Id);

            LCIMBinaryMessage binaryMessage = new LCIMBinaryMessage(System.Text.Encoding.UTF8.GetBytes("LeanCloud"));
            await conversation.Send(binaryMessage);
            Assert.NotNull(binaryMessage.Id);

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
            ReadOnlyCollection<LCIMMessage> messages = await conversation.QueryMessages(messageType: -6);
            Assert.Greater(messages.Count, 0);
            foreach (LCIMMessage message in messages) {
                Assert.AreEqual(message.ConversationId, conversation.Id);
                Assert.NotNull(message.Id);
                WriteLine(message.Id);
            }
        }

        [Test]
        [Order(5)]
        [Timeout(20000)]
        public async Task Unread() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            string clientId = Guid.NewGuid().ToString();
            LCIMClient client = new LCIMClient(clientId);
            LCIMConversation conversation = await m1.CreateConversation(new string[] { clientId });

            LCIMTextMessage textMessage = new LCIMTextMessage("hello");
            await conversation.Send(textMessage);

            client.OnUnreadMessagesCountUpdated = (convs) => {
                foreach (LCIMConversation conv in convs) {
                    WriteLine($"unread count: {conv.Unread}");
                    //Assert.AreEqual(conv.Unread, 1);
                    //Assert.True(conv.LastMessage is LCIMTextMessage);
                    //LCIMTextMessage textMsg = conv.LastMessage as LCIMTextMessage;
                    //Assert.AreEqual(textMsg.Text, "hello");
                }
            };
            await client.Open();

            client.OnMessage = (conv, msg) => {
                WriteLine($"unread count: {conv.Unread}");
                Assert.AreEqual(conv.Unread, 2);
                Assert.True(conv.LastMessage is LCIMTextMessage);
                LCIMTextMessage textMsg = conv.LastMessage as LCIMTextMessage;
                Assert.AreEqual(textMsg.Text, "world");
                tcs.SetResult(true);
            };
            textMessage = new LCIMTextMessage("world");
            await conversation.Send(textMessage);

            await tcs.Task;
        }

        [Test]
        [Order(6)]
        public async Task Attributes() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            m2.OnMessage = (conv, msg) => {
                Assert.True(msg is LCIMTypedMessage);
                LCIMTypedMessage typedMsg = msg as LCIMTypedMessage;
                Assert.AreEqual(typedMsg["k1"], 123);
                Assert.True((bool)typedMsg["k2"]);
                Assert.AreEqual(typedMsg["k3"], "code");
                tcs.SetResult(null);
            };
            LCIMTextMessage textMsg = new LCIMTextMessage("hi");
            textMsg["k1"] = 123;
            textMsg["k2"] = true;
            textMsg["k3"] = "code";
            await conversation.Send(textMsg);

            await tcs.Task;
        }

        [Test]
        [Order(7)]
        public async Task Custom() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            // 注册自定义类型消息
            LCIMTypedMessage.Register(EmojiMessage.EmojiMessageType,
                () => new EmojiMessage());
            m2.OnMessage = (conv, msg) => {
                Assert.True(msg is EmojiMessage);
                EmojiMessage emojiMsg = msg as EmojiMessage;
                Assert.AreEqual(emojiMsg.Ecode, "#0123");
                tcs.SetResult(null);
            };
            EmojiMessage emojiMessage = new EmojiMessage {
                Ecode = "#0123"
            };
            await conversation.Send(emojiMessage);

            await tcs.Task;
        }

        [Test]
        [Order(8)]
        public async Task MentionList() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            m2.OnMessage = (conv, msg) => {
                Assert.True(msg.Mentioned);
                Assert.True(msg.MentionIdList.Contains(m2.Id));
                tcs.SetResult(null);
            };

            LCIMTextMessage textMessage = new LCIMTextMessage("hello") {
                MentionIdList = new List<string> { m2.Id }
            };
            await conversation.Send(textMessage);

            await tcs.Task;
        }

        [Test]
        [Order(9)]
        public async Task MentionAll() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            m2.OnMessage = (conv, msg) => {
                Assert.True(msg.Mentioned);
                Assert.True(msg.MentionAll);
                tcs.SetResult(null);
            };

            LCIMTextMessage textMessage = new LCIMTextMessage("world") {
                MentionAll = true
            };
            await conversation.Send(textMessage);

            await tcs.Task;
        }
    }
}
