using System;
using System.Collections.Generic;
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
            if (Location == null) {
                throw new ArgumentNullException(nameof(Location));
            }
            Dictionary<string, object> data = base.Encode();
            Dictionary<string, object> locationData = new Dictionary<string, object> {
                { MessageDataLongitudeKey, Location.Longitude },
                { MessageDataLatitudeKey, Location.Latitude }
            };
            data[MessageLocationKey] = locationData;
            return data;
        }

        internal override void Decode(Dictionary<string, object> msgData) {
            base.Decode(msgData);
            if (msgData.TryGetValue(MessageLocationKey, out object val)) {
                Dictionary<string, object> locationData = val as Dictionary<string, object>;
                if (locationData == null) {
                    return;
                }
                double latitude = 0, longitude = 0;
                if (locationData.TryGetValue(MessageDataLatitudeKey, out object lat) &&
                    double.TryParse(lat as string, out double la)) {
                    latitude = la;
                }
                if (locationData.TryGetValue(MessageDataLongitudeKey, out object lon) &&
                    double.TryParse(lon as string, out double lo)) {
                    longitude = lo;
                }
                Location = new LCGeoPoint(latitude, longitude);
            }
        }

        public override int MessageType => LocationMessageType;
    }
}
