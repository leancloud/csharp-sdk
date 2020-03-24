using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Storage.Internal;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Realtime {
    public abstract class LCIMTypedMessage : LCIMMessage {
        private Dictionary<string, object> customProperties;

        internal virtual int MessageType {
            get; private set;
        }

        public object this[string key] {
            get {
                if (customProperties == null) {
                    return null;
                }
                return customProperties[key];
            }
            set {
                if (customProperties == null) {
                    customProperties = new Dictionary<string, object>();
                }
                customProperties[key] = value;
            }
        }

        protected LCIMTypedMessage() {
        }

        internal virtual Dictionary<string, object> Encode() {
            Dictionary<string, object> msgData = new Dictionary<string, object> {
                { "_lctype", MessageType }
            };
            if (customProperties != null && customProperties.Count > 0) {
                msgData["_lcattrs"] = LCEncoder.Encode(customProperties);
            }
            return msgData;
        }

        internal override void Decode(DirectCommand direct) {
            base.Decode(direct);
            Dictionary<string, object> msgData = JsonConvert.DeserializeObject<Dictionary<string, object>>(direct.Msg, new LCJsonConverter());
            DecodeMessageData(msgData);
        }

        protected virtual void DecodeMessageData(Dictionary<string, object> msgData) {
            MessageType = (int)msgData["_lctype"];
            if (msgData.TryGetValue("_lcattrs", out object attrObj)) {
                customProperties = LCDecoder.Decode(attrObj) as Dictionary<string, object>;
            }
        }

        internal static LCIMTypedMessage Deserialize(Dictionary<string, object> messageData) {
            LCIMTypedMessage message = null;
            int msgType = (int)(long)messageData["_lctype"];
            switch (msgType) {
                case -1:
                    message = new LCIMTextMessage();
                    break;
                case -2:
                    message = new LCIMImageMessage();
                    break;
                case -3:
                    message = new LCIMAudioMessage();
                    break;
                case -4:
                    message = new LCIMVideoMessage();
                    break;
                case -5:
                    message = new LCIMLocationMessage();
                    break;
                case -6:
                    message = new LCIMFileMessage();
                    break;
                default:
                    break;
            }
            //message.Decode(direct);
            return message;
        }
    }
}
