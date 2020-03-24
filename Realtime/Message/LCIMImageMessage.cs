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
            if (File.MetaData.TryGetValue("width", out object width)) {
                metaData["width"] = width;
            }
            if (File.MetaData.TryGetValue("height", out object height)) {
                metaData["height"] = height;
            }
            return data;
        }

        internal override int MessageType => ImageMessageType;
    }
}
