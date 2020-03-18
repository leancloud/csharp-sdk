﻿using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    public class LCIMAudioMessage : LCIMFileMessage {
        public double Duration {
            get {
                if (double.TryParse("duration", out double duration)) {
                    return duration;
                }
                return 0;
            }
        }

        public LCIMAudioMessage(LCFile file) : base(file) {
        }
    }
}
