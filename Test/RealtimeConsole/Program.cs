using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Common;
using LeanCloud.Storage;
using LeanCloud.Realtime;

namespace RealtimeConsole {
    class MainClass {
        public static void Main(string[] args) {
            Console.WriteLine("Hello World!");

            _ = Start();

            Console.ReadKey(true);
        }

        static async Task Start() {
            LCLogger.LogDelegate += (level, info) => {
                switch (level) {
                    case LCLogLevel.Debug:
                        Console.WriteLine($"[DEBUG] {info}");
                        break;
                    case LCLogLevel.Warn:
                        Console.WriteLine($"[WARNING] {info}");
                        break;
                    case LCLogLevel.Error:
                        Console.WriteLine($"[ERROR] {info}");
                        break;
                    default:
                        Console.WriteLine(info);
                        break;
                }
            };
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");

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

            world.OnMessageReceived = (conv, message) => {
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
