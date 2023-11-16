using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using LC.Newtonsoft.Json;
using LeanCloud;
using LeanCloud.Storage;
using LeanCloud.LiveQuery;

using static NUnit.Framework.TestContext;

namespace LiveQuery.Test {
    internal class Account : LCObject {
        internal int Balance {
            get {
                return (int)this["balance"];
            }
            set {
                this["balance"] = value;
            }
        }

        internal Account() : base("Account") { }
    }

    public class LiveQuery {
        private LCLiveQuery liveQuery;

        private Account account;

        [SetUp]
        public async Task Setup() {
            LCLogger.LogDelegate += Print;
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz",
                "NUKmuRbdAhg1vrb2wexYo1jo",
                "https://ikggdre2.lc-cn-n1-shared.com");

            LCObject.RegisterSubclass("Account", () => new Account());
            
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            query.WhereGreaterThan("balance", 100);
            liveQuery = await query.Subscribe();
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Print;
        }

        [Test]
        [Order(0)]
        public async Task Create() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            liveQuery.OnCreate = (obj) => {
                WriteLine($"******** create: {obj}");
                tcs.SetResult(null);
            };
            account = new Account {
                Balance = 110
            };
            await account.Save();

            await tcs.Task;
        }

        [Test]
        [Order(1)]
        public async Task Update() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            liveQuery.OnUpdate = (obj, updatedKeys) => {
                WriteLine($"******** update: {obj}");
                tcs.SetResult(null);
            };
            account.Balance = 120;
            await account.Save();

            await tcs.Task;
        }

        [Test]
        [Order(2)]
        public async Task Leave() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            liveQuery.OnLeave = (obj, updatedKeys) => {
                WriteLine($"******** level: {obj}");
                tcs.SetResult(null);
            };
            account.Balance = 80;
            await account.Save();

            await tcs.Task;
        }

        [Test]
        [Order(3)]
        public async Task Enter() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            liveQuery.OnEnter = (obj, updatedKeys) => {
                WriteLine($"******** enter: {obj}");
                tcs.SetResult(null);
            };
            account.Balance = 120;
            await account.Save();

            await tcs.Task;
        }

        [Test]
        [Order(4)]
        public async Task Delete() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            liveQuery.OnDelete = (objId) => {
                WriteLine($"******** delete: {objId}");
                tcs.SetResult(null);
            };
            await account.Delete();

            await tcs.Task;
        }

        [Test]
        [Order(5)]
        public async Task Login() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            await LCUser.Login("hello", "world");
            LCQuery<LCUser> userQuery = LCUser.GetQuery();
            userQuery.WhereEqualTo("username", "hello");
            LCLiveQuery userLiveQuery = await userQuery.Subscribe();
            userLiveQuery.OnLogin = (user) => {
                WriteLine($"login: {user}");
                tcs.SetResult(null);
            };

            // 模拟 REST API
            string url = "https://ikggdre2.lc-cn-n1-shared.com/1.1/login";
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post
            };
            request.Headers.Add("X-LC-Id", "ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz");
            request.Headers.Add("X-LC-Key", "NUKmuRbdAhg1vrb2wexYo1jo");
            string content = JsonConvert.SerializeObject(new Dictionary<string, object> {
                { "username", "hello" },
                { "password", "world" }
            });
            StringContent requestContent = new StringContent(content);
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = requestContent;

            using (HttpClient client = new HttpClient()) {
                await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                request.Dispose();
            }

            await tcs.Task;
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