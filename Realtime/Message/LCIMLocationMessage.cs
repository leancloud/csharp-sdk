using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    public class LCIMLocationMessage : LCIMTextMessage {
        public LCGeoPoint Location {
            get; set;
        }

        internal LCIMLocationMessage() {
        }

        public LCIMLocationMessage(LCGeoPoint locaction) : base(null) {
            Location = locaction;
        }

        internal override Dictionary<string, object> Encode() {
            Dictionary<string, object> data = base.Encode();
            Dictionary<string, object> locationData = new Dictionary<string, object> {
                { "longitude", Location.Longitude },
                { "latitude", Location.Latitude }
            };
            data["_lcloc"] = locationData;
            return data;
        }

        protected override void DecodeMessageData(Dictionary<string, object> msgData) {
            base.DecodeMessageData(msgData);
            Dictionary<string, object> locationData = msgData["_lcloc"] as Dictionary<string, object>;
            Location = new LCGeoPoint((double)locationData["latitude"], (double)locationData["longitude"]);
        }

        internal override int MessageType => LocationMessageType;
    }
}
