using System.Collections.Generic;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMAudioMessage is a local representation of audio message in LeanCloud.
    /// </summary>
    public class LCIMAudioMessage : LCIMFileMessage {
        public double Duration {
            get; private set;
        }

        internal LCIMAudioMessage() {
        }

        public LCIMAudioMessage(LCFile file) : base(file) {
            
        }

        internal override Dictionary<string, object> Encode() {
            Dictionary<string, object> data = base.Encode();
            Dictionary<string, object> fileData = data[MessageFileKey] as Dictionary<string, object>;
            Dictionary<string, object> metaData = fileData[MessageDataMetaDataKey] as Dictionary<string, object>;
            if (File.MetaData != null &&
                File.MetaData.TryGetValue(MessageDataMetaDurationKey, out object duration)) {
                metaData[MessageDataMetaDurationKey] = duration;
            }
            return data;
        }

        internal override void Decode(Dictionary<string, object> msgData) {
            base.Decode(msgData);

            if (File.MetaData == null) {
                return;
            }
            if (File.MetaData.TryGetValue(MessageDataMetaDurationKey, out object duration) &&
                double.TryParse(duration as string, out double d)) {
                Duration = d;
            }
        }

        public override int MessageType => AudioMessageType;
    }
}
