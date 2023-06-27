using LeanCloud.Engine;

namespace web {
    public partial class App {
        [LCEngineFunction("ping")]
        public static string Ping() {
            return "pong";
        }
    }
}
