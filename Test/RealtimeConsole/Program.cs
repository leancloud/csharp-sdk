using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Common;
using LeanCloud.Realtime;

namespace RealtimeConsole {
    class MainClass {
        public static void Main(string[] args) {
            Console.WriteLine("Hello World!");

            Start();

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

            LCIMClient client = new LCIMClient("hello123");

            try {
                await client.Open();
                Console.WriteLine($"End {Thread.CurrentThread.ManagedThreadId}");
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            client.OnInvited = (conv, initBy) => {
                Console.WriteLine($"on invited: {initBy}");
            };

            client.OnMembersJoined = (conv, memberList, initBy) => {
                Console.WriteLine($"on members joined: {initBy}");
            };

            List<string> memberIdList = new List<string> { "world", "code" };
            string name = Guid.NewGuid().ToString();
            LCIMConversation conversation = await client.CreateConversation(memberIdList, name: name, unique: true);

            LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
            await conversation.Send(textMessage);

            await Task.Delay(3000);

            LCIMTextMessage newMessage = new LCIMTextMessage("hello, code");
            await conversation.Update(textMessage, newMessage);

            //// 设置成员的角色
            //await conversation.UpdateMemberRole("world", LCIMConversationMemberInfo.Manager);

            //List<LCIMConversationMemberInfo> members = await conversation.GetAllMemberInfo();

            //foreach (LCIMConversationMemberInfo member in members) {
            //    Console.WriteLine(member.MemberId);
            //}

            //LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
            //await conversation.Send(textMessage);
        }
    }
}
