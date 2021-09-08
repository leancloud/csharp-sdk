using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
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
    public class Message : BaseTest {
        private LCIMClient m1;
        private LCIMClient m2;

        private LCIMConversation conversation;

        [SetUp]
        public override async Task SetUp() {
            await base.SetUp();
            m1 = new LCIMClient("m1");
            m2 = new LCIMClient("m2");
            await m1.Open();
            await m2.Open();
            conversation = await m1.CreateConversation(new string[] { "m2" });
        }

        [TearDown]
        public override async Task TearDown() {
            await m1.Close();
            await m2.Close();
            await base.TearDown();
        }

        [Test]
        [Order(0)]
        [Timeout(20000)]
        public async Task Send() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            int count = 0;
            m2.OnMessage = (conv, msg) => {
                WriteLine(msg.Id);
                if (msg is LCIMImageMessage imageMsg) {
                    WriteLine($"-------- url: {imageMsg.Url}");
                    Assert.NotNull(imageMsg.Url);
                    Assert.AreEqual(imageMsg.Width, 225);
                    Assert.AreEqual(imageMsg.Height, 225);
                    count++;
                } else if (msg is LCIMLocationMessage locationMsg) {
                    WriteLine($"-------- location: {locationMsg.Location}");
                    Assert.Less(Math.Abs(20.0059 - locationMsg.Location.Latitude), 0.0001);
                    Assert.Less(Math.Abs(110.3665 - locationMsg.Location.Longitude), 0.0001);
                    count++;
                } else if (msg is LCIMAudioMessage audioMsg) {
                    WriteLine($"-------- audio: {audioMsg.File}");
                    Assert.NotNull(audioMsg.File.Url);
                    Assert.Less(Math.Abs(audioMsg.Duration - 1), 0.0001);
                    count++;
                } else if (msg is LCIMVideoMessage videoMsg) {
                    WriteLine($"-------- video: {videoMsg}");
                    Assert.NotNull(videoMsg.File.Url);
                    Assert.Less(Math.Abs(videoMsg.Duration - 11), 0.0001);
                    Assert.AreEqual(videoMsg.Width, 1280);
                    Assert.AreEqual(videoMsg.Height, 720);
                    count++;
                } else if (msg is LCIMFileMessage fileMsg) {
                    WriteLine($"-------- name: {fileMsg.Format}");
                    Assert.NotNull(fileMsg.Url);
                    Assert.AreEqual(fileMsg.Format, "zip");
                    Assert.AreEqual(fileMsg.Size, 1387);
                    count++;
                } else if (msg is LCIMTextMessage textMsg) {
                    WriteLine($"-------- text: {textMsg.Text}");
                    Assert.AreEqual(textMsg.Text, "hello, world");
                    count++;
                } 
                if (count == 6) {
                    tcs.SetResult(null);
                }
            };

            LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
            await conversation.Send(textMessage);
            Assert.NotNull(textMessage.Id);

            LCFile image = new LCFile("hello", "../../../../../assets/hello.png");
            image.AddMetaData("width", 225);
            image.AddMetaData("height", 225);
            LCIMImageMessage imageMessage = new LCIMImageMessage(image);
            await conversation.Send(imageMessage);
            Assert.NotNull(imageMessage.Id);

            LCFile file = new LCFile("hello.zip", "../../../../../assets/hello.png.zip");
            file.AddMetaData("size", 1387);
            LCIMFileMessage fileMessage = new LCIMFileMessage(file);
            await conversation.Send(fileMessage);
            Assert.NotNull(fileMessage.Id);

            LCIMBinaryMessage binaryMessage = new LCIMBinaryMessage(System.Text.Encoding.UTF8.GetBytes("LeanCloud"));
            await conversation.Send(binaryMessage);
            Assert.NotNull(binaryMessage.Id);

            LCIMLocationMessage locationMessage = new LCIMLocationMessage(new LCGeoPoint(20.0059, 110.3665));
            await conversation.Send(locationMessage);
            Assert.NotNull(locationMessage.Id);

            LCFile audio = new LCFile("audio.wav", "../../../../../assets/audio.wav");
            audio.AddMetaData("duration", 1.0);
            LCIMAudioMessage audioMessage = new LCIMAudioMessage(audio);
            await conversation.Send(audioMessage);

            LCFile video = new LCFile("lake.mp4", "../../../../../assets/lake.mp4");
            video.AddMetaData("duration", 11.0);
            video.AddMetaData("width", 1280);
            video.AddMetaData("height", 720);
            LCIMVideoMessage videoMessage = new LCIMVideoMessage(video);
            await conversation.Send(videoMessage);

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
                    WriteLine($"OnUnreadMessagesCountUpdated unread count: {conv.Unread}");
                    Assert.AreEqual(conv.Unread, 1);
                    Assert.True(conv.LastMessage is LCIMTextMessage);
                    LCIMTextMessage textMsg = conv.LastMessage as LCIMTextMessage;
                    Assert.AreEqual(textMsg.Text, "hello");
                    tcs.TrySetResult(null);
                }
            };
            await Task.Delay(5000);
            await client.Open();

            await tcs.Task;

            tcs = new TaskCompletionSource<object>();
            client.OnMessage = (conv, msg) => {
                WriteLine($"OnMessage unread count: {conv.Unread}");
                Assert.AreEqual(conv.Unread, 2);
                Assert.True(conv.LastMessage is LCIMTextMessage);
                LCIMTextMessage textMsg = conv.LastMessage as LCIMTextMessage;
                Assert.AreEqual(textMsg.Text, "world");
                tcs.TrySetResult(null);
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

        [Test]
        [Order(10)]
        public async Task SendMessageConcurrently() {
            LCIMClient client = new LCIMClient("client");
            await client.Open();
            LCIMConversation conv = await client.CreateConversation(new string[] { "xxx" });
            LCIMTextMessage msg = new LCIMTextMessage("hello");
            for (int i = 0; i < 1000; i++) {
                _ = Task.Run(() => {
                    _ = conv.Send(msg);
                });
            }
            await Task.Delay(5000);
            await client.Close();
        }
    }
}
