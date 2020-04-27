using System.Collections.Generic;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 文本消息
    /// </summary>
    public class LCIMTextMessage : LCIMTypedMessage {
        /// <summary>
        /// 文本
        /// </summary>
        public string Text {
            get; set;
        }

        internal LCIMTextMessage() {
        }

        public LCIMTextMessage(string text) : base() {
            Text = text;
        }

        internal override Dictionary<string, object> Encode() {
            Dictionary<string, object> data = base.Encode();
            if (!string.IsNullOrEmpty(Text)) {
                data[MessageTextKey] = Text;
            }
            return data;
        }

        public override int MessageType => TextMessageType;

        internal override void Decode(Dictionary<string, object> msgData) {
            base.Decode(msgData);
            if (msgData.TryGetValue(MessageTextKey, out object value)) {
                Text = value as string;
            }
        }
    }
}
