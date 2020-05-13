using System;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Storage;
using LeanCloud.LiveQuery;

using static System.Console;

namespace LiveQueryApp {
    class Program {
        static void Main(string[] args) {
            WriteLine("Hello World!");

            SingleThreadSynchronizationContext.Run(async () => {
                LCLogger.LogDelegate += Print;
                LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz",
                    "NUKmuRbdAhg1vrb2wexYo1jo",
                    "https://ikggdre2.lc-cn-n1-shared.com");

                await LCUser.Login("hello", "world");
                LCQuery<LCUser> userQuery = LCUser.GetQuery();
                userQuery.WhereEqualTo("username", "hello");
                LCLiveQuery userLiveQuery = await userQuery.Subscribe();
                userLiveQuery.OnLogin = (user) => {
                    WriteLine($"login: {user.Username}");
                };

                LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
                query.WhereGreaterThan("balance", 100);
                LCLiveQuery liveQuery = await query.Subscribe();
                liveQuery.OnCreate = (obj) => {
                    WriteLine($"create: {obj}");
                };
                liveQuery.OnUpdate = (obj, keys) => {
                    WriteLine($"update: {obj}");
                    WriteLine(keys.Count);
                };
                liveQuery.OnDelete = (objId) => {
                    WriteLine($"delete: {objId}");
                };
                liveQuery.OnEnter = (obj, keys) => {
                    WriteLine($"enter: {obj}");
                    WriteLine(keys.Count);
                };
                liveQuery.OnLeave = (obj, keys) => {
                    WriteLine($"leave: {obj}");
                    WriteLine(keys.Count);
                };
            });
        }

        private static void Print(LCLogLevel level, string info) {
            switch (level) {
                case LCLogLevel.Debug:
                    WriteLine($"[DEBUG] {info}\n");
                    break;
                case LCLogLevel.Warn:
                    WriteLine($"[WARNING] {info}\n");
                    break;
                case LCLogLevel.Error:
                    WriteLine($"[ERROR] {info}\n");
                    break;
                default:
                    WriteLine(info);
                    break;
            }
        }
    }
}
