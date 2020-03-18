using System.Collections.Generic;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    public class LCIMAudioMessage : LCIMFileMessage {
        public double Duration {
            get {
                if (double.TryParse(File.MetaData["duration"] as string, out double duration)) {
                    return duration;
                }
                return 0;
            }
        }

        internal LCIMAudioMessage() {
        }

        public LCIMAudioMessage(LCFile file) : base(file) {
            
        }

        internal override Dictionary<string, object> Encode() {
            Dictionary<string, object> data = base.Encode();
            Dictionary<string, object> fileData = data["_lcfile"] as Dictionary<string, object>;
            Dictionary<string, object> metaData = fileData["metaData"] as Dictionary<string, object>;
            metaData["duration"] = File.MetaData["duration"];
            return data;
        }

        internal override int MessageType => AudioMessageType;
    }
}
