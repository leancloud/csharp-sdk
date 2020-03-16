using System;
using Newtonsoft.Json;

namespace LeanCloud.Realtime {
    public class LCIMTextMessage : LCIMTypedMessage {
        const int TextMessageType = -1;

        private string text;

        public LCIMTextMessage(string text) : base(TextMessageType) {
            this.text = text;
        }

        internal override string Serialize() {
            return text;
        }
    }
}
