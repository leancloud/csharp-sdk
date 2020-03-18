using System.Collections.Generic;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    public class LCIMImageMessage : LCIMFileMessage {
        public int Width {
            get {
                if (int.TryParse(File.MetaData["width"] as string, out int width)) {
                    return width;
                }
                return 0;
            }
        }

        public int Height {
            get {
                if (int.TryParse(File.MetaData["height"] as string, out int height)) {
                    return height;
                }
                return 0;
            }
        }

        internal LCIMImageMessage() : base() {
        }

        public LCIMImageMessage(LCFile file) : base(file) {

        }

        internal override Dictionary<string, object> Encode() {
            Dictionary<string, object> data = base.Encode();
            Dictionary<string, object> fileData = data["_lcfile"] as Dictionary<string, object>;
            Dictionary<string, object> metaData = fileData["metaData"] as Dictionary<string, object>;
            metaData["width"] = File.MetaData["width"];
            metaData["height"] = File.MetaData["height"];
            return data;
        }

        internal override int MessageType => ImageMessageType;
    }
}
