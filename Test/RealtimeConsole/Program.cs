using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Common;
using LeanCloud.Realtime;

namespace RealtimeConsole {
    class MainClass {
        public static void Main(string[] args) {
            Console.WriteLine($"Hello World at {Thread.CurrentThread.ManagedThreadId}");

            LCLogger.LogDelegate += (level, info) => {
                switch (level) {
                    case LCLogLevel.Debug:
                        Console.WriteLine($"[DEBUG]\n{info}");
                        break;
                    case LCLogLevel.Warn:
                        Console.WriteLine($"[WARNING]\n{info}");
                        break;
                    case LCLogLevel.Error:
                        Console.WriteLine($"[ERROR]\n{info}");
                        break;
                    default:
                        Console.WriteLine(info);
                        break;
                }
            };
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");

            SingleThreadSynchronizationContext.Run(async () => {
                Console.WriteLine($"start at {Thread.CurrentThread.ManagedThreadId}");
                await Run("cc4");
                //await ChatRoom();
                //await TemporaryConversation();
                //await CreateConversation();
                //await QueryMyConversation();
                Console.WriteLine($"done at {Thread.CurrentThread.ManagedThreadId}");
            });

            //Conversation().Wait();

            //_ = Signature();

            //_ = Block();

            //_ = Mute();

            //QueryConversation().Wait();

            //_ = OpenAndClose();

            //SendMessage().Wait();

            //Unread().Wait();

            //DemoAsync().Wait();

            //SingleThreadSynchronizationContext.Run(async () => {
            //    await DemoAsync();
            //});

            Console.ReadKey(true);
        }

        static async Task DemoAsync() {
            Dictionary<int, int> d = new Dictionary<int, int>();
            for (int i = 0; i < 10000; i++) {
                int id = Thread.CurrentThread.ManagedThreadId;
                int count;
                d[id] = d.TryGetValue(id, out count) ? count + 1 : 1;
                await Task.Yield();
            }
            foreach (KeyValuePair<int, int> kv in d) {
                Console.WriteLine(kv);
            }
        }

        static async Task Run(string id) {
            LCIMClient client = new LCIMClient(id);
            await client.Open();
            client.OnUnreadMessagesCountUpdated = (conversations) => {
                foreach (LCIMConversation conv in conversations) {
                    Console.WriteLine($"unread: {conv.Id}");
                }
            };
            client.OnMessage = (conversation, message) => {
                Console.WriteLine($"recv: {conversation.Id}, {message.Id} at {Thread.CurrentThread.ManagedThreadId}");
            };
        }

        static async Task CreateConversation() {
            LCIMClient cc1 = new LCIMClient("cc1");
            await cc1.Open();
            //await cc1.CreateChatRoom("leancloud chat");
            await cc1.CreateTemporaryConversation(new string[] { "cc2", "cc3" });
            //await cc1.CreateConversation(new string[] { "cc4" });
        }

        static async Task QueryMyConversation() {
            LCIMClient cc1 = new LCIMClient("cc1");
            await cc1.Open();
            ReadOnlyCollection<LCIMConversation> conversationList = await cc1.GetQuery()
                .WhereEqualTo("objectId", "5e7c283790aef5aa846b5683")
                .Find();
            foreach (LCIMConversation conv in conversationList) {
                Console.WriteLine($"convId: {conv.Id}");
            }
        }

        static async Task Unread() {
            LCIMClient u2 = new LCIMClient("u2");
            await u2.Open();
            u2.OnUnreadMessagesCountUpdated = conversationList => {
                foreach (LCIMConversation conv in conversationList) {
                    Console.WriteLine($"unread: {conv.Unread}");
                }
            };
        }

        static async Task SendMessage() {
            Console.WriteLine($"start at {Thread.CurrentThread.ManagedThreadId}");
            try {
                LCIMClient u1 = new LCIMClient("u1");
                await u1.Open();
                LCIMConversation conversation = await u1.CreateConversation(new string[] { "u2" });

                Console.WriteLine($"open at {Thread.CurrentThread.ManagedThreadId}");

                LCIMTextMessage textMessage = new LCIMTextMessage("hello, text message");
                await conversation.Send(textMessage);

                Console.WriteLine($"send at {Thread.CurrentThread.ManagedThreadId}");

                //LCFile file = new LCFile("avatar", "../../../Storage.Test/assets/hello.png");
                //await file.Save();
                //LCIMImageMessage imageMessage = new LCIMImageMessage(file);
                //await conversation.Send(imageMessage);
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        static async Task OpenAndClose() {
            LCIMClient o1 = new LCIMClient("o1");
            await o1.Open();
            await o1.Close();
        }

        static async Task QueryConversation() {
            LCIMClient m2 = new LCIMClient("m2");
            await m2.Open();

            LCIMConversation conv = (await m2.GetQuery()
                .WhereEqualTo("objectId", "5e7863bf90aef5aa849be75a")
                .Find())[0];
            LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
            await conv.Send(textMessage);
        }

        static async Task Mute() {
            LCIMClient m1 = new LCIMClient("m0");
            await m1.Open();

            LCIMClient m2 = new LCIMClient("m2");
            await m2.Open();

            LCIMConversation conversation = await m1.CreateConversation(new string[] { "m2", "m3" });
            await conversation.MuteMembers(new string[] { "m2" });

            LCIMConversation conv = (await m2.GetQuery()
                .WhereEqualTo("objectId", conversation.Id)
                .Find())[0];
            LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
            await conv.Send(textMessage);
        }

        static async Task Block() {
            LocalSignatureFactory signatureFactory = new LocalSignatureFactory();
            LCIMClient c1 = new LCIMClient("c0");
            await c1.Open();
            LCIMConversation conversation = await c1.CreateConversation(new string[] { "c2", "c3", "c4", "c5" });
            LCIMTextMessage textMessage = new LCIMTextMessage("hello");
            await conversation.Send(textMessage);
            await conversation.BlockMembers(new string[] { "c5" });

            LCIMClient c5 = new LCIMClient("c5");
            await c5.Open();
            await conversation.AddMembers(new string[] { "c5" });
        }

        static async Task Signature() {
            LocalSignatureFactory signatureFactory = new LocalSignatureFactory();
            LCIMClient hello = new LCIMClient("hello111", signatureFactory);
            await hello.Open();
        }

        static async Task ChatRoom() {
            LocalSignatureFactory signatureFactory = new LocalSignatureFactory();
            LCIMClient hello = new LCIMClient("hello", signatureFactory);
            await hello.Open();

            string name = Guid.NewGuid().ToString();
            LCIMChatRoom chatRoom = await hello.CreateChatRoom(name);
            Console.WriteLine(chatRoom.Name);

            await chatRoom.AddMembers(new string[] { "world" });

            await chatRoom.RemoveMembers(new string[] { "world" });
        }

        static async Task TemporaryConversation() {
            string c1Id = Guid.NewGuid().ToString();
            LCIMClient c1 = new LCIMClient(c1Id);
            await c1.Open();

            string c2Id = Guid.NewGuid().ToString();
            LCIMClient c2 = new LCIMClient(c2Id);
            await c2.Open();

            LCIMTemporaryConversation temporaryConversation = await c1.CreateTemporaryConversation(new string[] { c2Id });
            Console.WriteLine(temporaryConversation.Id);
        }

        static async Task Conversation() {
            LCIMClient hello = new LCIMClient("hello");

            await hello.Open();

            hello.OnInvited = (conv, initBy) => {
                Console.WriteLine($"on invited: {initBy}");
            };

            hello.OnMembersJoined = (conv, memberList, initBy) => {
                Console.WriteLine($"on members joined: {initBy}");
            };

            List<string> memberIdList = new List<string> { "world", "code" };
            string name = Guid.NewGuid().ToString();
            LCIMConversation conversation = await hello.CreateConversation(memberIdList, name: name, unique: true);

            LCIMClient world = new LCIMClient("world");
            await world.Open();

            world.OnMessage = (conv, message) => {
                Console.WriteLine(message);
                if (message is LCIMTypedMessage typedMessage) {
                    Console.WriteLine(typedMessage["k1"]);
                    Console.WriteLine(typedMessage["k2"]);
                    Console.WriteLine(typedMessage["k3"]);
                }
            };

            //LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
            //await conversation.Send(textMessage);

            //await Task.Delay(3000);

            //LCIMTextMessage newMessage = new LCIMTextMessage("hello, code");
            //await conversation.Update(textMessage, newMessage);

            //// 设置成员的角色
            //await conversation.UpdateMemberRole("world", LCIMConversationMemberInfo.Manager);

            //List<LCIMConversationMemberInfo> members = await conversation.GetAllMemberInfo();

            //foreach (LCIMConversationMemberInfo member in members) {
            //    Console.WriteLine(member.MemberId);
            //}

            LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
            textMessage["k1"] = 123;
            textMessage["k2"] = "abc";
            textMessage["k3"] = true;
            await conversation.Send(textMessage);

            //LCFile file = new LCFile("avatar", "../../../Storage.Test/assets/hello.png");
            //file.MetaData["width"] = 225;
            //file.MetaData["height"] = 225;
            //file.MetaData["size"] = 1186;
            //await file.Save();
            //LCIMImageMessage imageMessage = new LCIMImageMessage(file);
            //await conversation.Send(imageMessage);

            //LCGeoPoint location = new LCGeoPoint(11, 12);
            //LCIMLocationMessage locationMessage = new LCIMLocationMessage(location);
            //await conversation.Send(locationMessage);
        }
    }
}
