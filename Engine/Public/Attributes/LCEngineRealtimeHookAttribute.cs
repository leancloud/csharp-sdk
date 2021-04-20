using System;

namespace LeanCloud.Engine {
    public enum LCEngineRealtimeHookType {
        // 消息
        MessageReceived,
        MessageSent,
        MessageUpdate,
        ReceiversOffline,
        // 对话
        ConversationStart,
        ConversationStarted,
        ConversationAdd,
        ConversationAdded,
        ConversationRemove,
        ConversationRemoved,
        ConversationUpdate,
        // 客户端
        ClientOnline,
        ClientOffline,
    }

    /// <summary>
    /// LCEngineRealtimeHookAttribute is an attribute that hooks realtime in engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LCEngineRealtimeHookAttribute : Attribute {
        public LCEngineRealtimeHookType HookType {
            get;
        }

        public LCEngineRealtimeHookAttribute(LCEngineRealtimeHookType hookType) {
            HookType = hookType;
        }
    }
}
