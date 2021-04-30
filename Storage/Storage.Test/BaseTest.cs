using NUnit.Framework;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    internal class Hello : LCObject {
        internal World World {
            get => this["objectValue"] as World;
            set {
                this["objectValue"] = value;
            }
        }

        internal Hello() : base("Hello") { }
    }

    internal class World : LCObject {
        internal string Content {
            get => this["content"] as string;
            set {
                this["content"] = value;
            }
        }

        internal World() : base("World") { }
    }

    internal class Account : LCObject {
        internal int Balance {
            get => (int)this["balance"];
            set {
                this["balance"] = value;
            }
        }

        internal LCUser User {
            get => this["user"] as LCUser;
            set {
                this["user"] = value;
            }
        }

        internal Account() : base("Account") { }
    }

    public class BaseTest {
        internal const string AppId = "ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz";
        internal const string AppKey = "NUKmuRbdAhg1vrb2wexYo1jo";
        internal const string MasterKey = "pyvbNSh5jXsuFQ3C8EgnIdhw";
        internal const string AppServer = "https://ikggdre2.lc-cn-n1-shared.com";

        internal const string TestPhone = "18888888888";
        internal const string TestSMSCode = "235750";

        [SetUp]
        public virtual void SetUp() {
            LCLogger.LogDelegate += Print;
            LCApplication.Initialize(AppId, AppKey, AppServer);
            LCObject.RegisterSubclass("Account", () => new Account());
            LCObject.RegisterSubclass("Hello", () => new Hello());
            LCObject.RegisterSubclass("World", () => new World());
        }

        [TearDown]
        public virtual void TearDown() {
            LCLogger.LogDelegate -= Print;
        }

        internal static void Print(LCLogLevel level, string info) {
            switch (level) {
                case LCLogLevel.Debug:
                    TestContext.Out.WriteLine($"[DEBUG] {info}");
                    break;
                case LCLogLevel.Warn:
                    TestContext.Out.WriteLine($"[WARNING] {info}");
                    break;
                case LCLogLevel.Error:
                    TestContext.Out.WriteLine($"[ERROR] {info}");
                    break;
                default:
                    TestContext.Out.WriteLine(info);
                    break;
            }
        }
    }
}
