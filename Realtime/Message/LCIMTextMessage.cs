using System.Collections.Generic;

namespace LeanCloud.Realtime {
    public class LCIMTextMessage : LCIMTypedMessage {
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
                data["_lctext"] = Text;
            }
            return data;
        }

        internal override int MessageType => TextMessageType;

        protected override void DecodeMessageData(Dictionary<string, object> msgData) {
            base.DecodeMessageData(msgData);
            if (msgData.TryGetValue("_lctext", out object value)) {
                Text = value as string;
            }
        }
    }
}
