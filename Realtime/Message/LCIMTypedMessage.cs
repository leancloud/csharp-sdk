using System;

namespace LeanCloud.Realtime {
    public class LCIMTypedMessage : LCIMMessage {
        protected int type;

        protected LCIMTypedMessage(int type) {
            this.type = type;
        }

        internal override string Serialize() {
            throw new NotImplementedException();
        }

        internal override string GetText() {
            return null;
        }

        internal override byte[] GetBytes() {
            return null;
        }
    }
}
