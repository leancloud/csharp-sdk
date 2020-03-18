using System;
using System.Collections.Generic;

namespace LeanCloud.Realtime {
    public class LCIMBinaryMessage : LCIMMessage {
        public byte[] Data {
            get; internal set;
        }

        public LCIMBinaryMessage(byte[] data) {
            Data = data;
        }
    }
}
