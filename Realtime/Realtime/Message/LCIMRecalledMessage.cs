
namespace LeanCloud.Realtime {
    /// <summary>
    /// The recall message, i.e. a message to recall a previous sent message.
    /// </summary>
    public class LCIMRecalledMessage : LCIMTypedMessage {
        public LCIMRecalledMessage() {
        }

        public override int MessageType => RecalledMessageType;
    }
}
