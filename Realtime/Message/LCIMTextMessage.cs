using System;
using Newtonsoft.Json;

namespace LeanCloud.Realtime {
    public class LCIMTextMessage : LCIMTypedMessage {
        const int TextMessageType = -1;

        public string Text {
            get; set;
        }

        public LCIMTextMessage(string text) : base(TextMessageType) {
            Text = text;
        }

        internal override string Serialize() {
            return Text;
        }

        internal override string GetText() {
            return Text;
        }
    }
}
