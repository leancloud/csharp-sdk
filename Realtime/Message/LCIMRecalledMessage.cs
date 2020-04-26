
namespace LeanCloud.Realtime {
    /// <summary>
    /// 撤回消息
    /// </summary>
    public class LCIMRecalledMessage : LCIMTypedMessage {
        public LCIMRecalledMessage() {
        }

        internal override int MessageType => RecalledMessageType;
    }
}
