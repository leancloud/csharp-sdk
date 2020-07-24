using System.Collections.Generic;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    public class LCIMVideoMessage : LCIMFileMessage {
        public int Width {
            get; private set;
        }

        public int Height {
            get; private set;
        }

        public double Duration {
            get; private set;
        }

        internal LCIMVideoMessage() {
        }

        public LCIMVideoMessage(LCFile file) : base(file) {

        }

        internal override Dictionary<string, object> Encode() {
            Dictionary<string, object> data = base.Encode();
            Dictionary<string, object> fileData = data[MessageFileKey] as Dictionary<string, object>;
            Dictionary<string, object> metaData = fileData[MessageDataMetaDataKey] as Dictionary<string, object>;
            if (File.MetaData != null) {
                if (File.MetaData.TryGetValue(MessageDataMetaWidthKey, out object width)) {
                    metaData[MessageDataMetaWidthKey] = width;
                }
                if (File.MetaData.TryGetValue(MessageDataMetaHeightKey, out object height)) {
                    metaData[MessageDataMetaHeightKey] = height;
                }
                if (File.MetaData.TryGetValue(MessageDataMetaDurationKey, out object duration)) {
                    metaData[MessageDataMetaDurationKey] = duration;
                }
            }
            return data;
        }

        internal override void Decode(Dictionary<string, object> msgData) {
            base.Decode(msgData);

            if (File.MetaData == null) {
                return;
            }
            if (File.MetaData.TryGetValue(MessageDataMetaWidthKey, out object width) &&
                int.TryParse(width as string, out int w)) {
                Width = w;
            }
            if (File.MetaData.TryGetValue(MessageDataMetaHeightKey, out object height) &&
                int.TryParse(height as string, out int h)) {
                Height = h;
            }
            if (File.MetaData.TryGetValue(MessageDataMetaDurationKey, out object duration) &&
                double.TryParse(duration as string, out double d)) {
                Duration = d;
            }
        }

        public override int MessageType => VideoMessageType;
    }
}
