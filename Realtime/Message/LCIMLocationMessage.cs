using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    public class LCIMLocationMessage : LCIMTextMessage {
        public LCGeoPoint Location {
            get; set;
        }

        public LCIMLocationMessage(LCGeoPoint locaction) : base(null) {
            Location = locaction;
        }

        internal override string Serialize() {
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "longitude", Location.Longitude },
                { "latitude", Location.Latitude }
            };
            return JsonConvert.SerializeObject(new Dictionary<string, object> {
                { "_lcloc", data }
            });
        }
    }
}
