using System;
using System.Collections.Generic;
using LeanCloud.Engine;

namespace web {
    public partial class App {
        // RTM Hook
        [LCEngineRealtimeHook(LCEngineRealtimeHookType.ConversationStart)]
        public static Dictionary<string, object> OnConversationStart(Dictionary<string, object> param) {
            foreach (KeyValuePair<string, object> kv in param) {
                Console.WriteLine($"{kv.Key} : {kv.Value}");
            }
            string initId = param["initBy"] as string;
            if ("forbidden_rtm_user".Equals(initId)) {
                Console.WriteLine($"Forbidden user: {initId}");
                return new Dictionary<string, object> {
                    { "reject", true },
                    { "code", 222 },
                    { "detail", "reject the conversation C#" }
                };
            }
            return default;
        }

        [LCEngineRealtimeHook(LCEngineRealtimeHookType.MessageReceived)]
        public static Dictionary<string, object> OnMessageReceived(LCEngineRequestContext context, Dictionary<string, object> param) {
            Console.WriteLine($"OnMessageReceived from {context.RemoteAddress}");
            foreach (KeyValuePair<string, object> kv in param) {
                Console.WriteLine($"{kv.Key} : {kv.Value}");
            }
            string fromId = param["fromPeer"] as string;
            if ("mute_rtm_user".Equals(fromId)) {
                Console.WriteLine($"Mute user: {fromId}");
                return new Dictionary<string, object> {
                    { "drop", true },
                    { "code", 333 },
                    { "detail", "mute the user C#" }
                };
            }
            return default;
        }
    }
}
