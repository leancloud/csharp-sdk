using System;

namespace LeanCloud.Realtime {
    public class LCIMBinaryMessage : LCIMMessage {
        public byte[] Data {
            get; set;
        }

        public LCIMBinaryMessage(byte[] data) {
            Data = data;
        }

        internal override string Serialize() {
            throw new NotImplementedException();
        }

        internal override string GetText() {
            return null;
        }

        internal override byte[] GetBytes() {
            return Data;
        }
    }
}
