using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Realtime {
    public abstract class LCIMTypedMessage : LCIMMessage {
        protected LCIMTypedMessage() {

        }

        internal virtual int MessageType {
            get; private set;
        }

        internal virtual Dictionary<string, object> Encode() {
            return new Dictionary<string, object> {
                { "_lctype", MessageType }
            };
        }

        internal override void Decode(DirectCommand direct) {
            base.Decode(direct);
            Dictionary<string, object> msgData = JsonConvert.DeserializeObject<Dictionary<string, object>>(direct.Msg, new LCJsonConverter());
            DecodeMessageData(msgData);
        }

        protected virtual void DecodeMessageData(Dictionary<string, object> msgData) {
            MessageType = (int)msgData["_lctype"];
        }
    }
}
