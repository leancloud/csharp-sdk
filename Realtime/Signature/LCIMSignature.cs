namespace LeanCloud.Realtime {
    public class LCIMSignature {
        public string Signature {
            get; set;
        }

        public long Timestamp {
            get; set;
        }

        public string Nonce {
            get; set;
        }

        public LCIMSignature() {
            
        }
    }
}
