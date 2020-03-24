using System.Collections.Generic;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    public class LCIMVideoMessage : LCIMFileMessage {
        public double Duration {
            get {
                if (double.TryParse(File.MetaData["duration"] as string, out double duration)) {
                    return duration;
                }
                return 0;
            }
        }

        internal LCIMVideoMessage() {
        }

        public LCIMVideoMessage(LCFile file) : base(file) {

        }

        internal override Dictionary<string, object> Encode() {
            Dictionary<string, object> data = base.Encode();
            Dictionary<string, object> fileData = data["_lcfile"] as Dictionary<string, object>;
            Dictionary<string, object> metaData = fileData["metaData"] as Dictionary<string, object>;
            if (File.MetaData.TryGetValue("width", out object width)) {
                metaData["width"] = width;
            }
            if (File.MetaData.TryGetValue("height", out object height)) {
                metaData["height"] = height;
            }
            if (File.MetaData.TryGetValue("duration", out object duration)) {
                metaData["duration"] = duration;
            }
            return data;
        }

        internal override int MessageType => VideoMessageType;
    }
}
