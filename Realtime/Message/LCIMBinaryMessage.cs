using System;

namespace LeanCloud.Realtime {
    public class LCIMBinaryMessage : LCIMMessage {
        private byte[] data;

        public LCIMBinaryMessage(byte[] data) {
            this.data = data;
        }

        internal override string Serialize() {
            throw new NotImplementedException();
        }
    }
}
