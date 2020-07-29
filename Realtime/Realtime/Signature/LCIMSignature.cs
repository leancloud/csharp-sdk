namespace LeanCloud.Realtime {
    public class LCIMSignature {
        public string Signature {
            get; set;
        }

        public long Timestamp {
            get; set;
        }

        /// <summary>
        /// A random string.
        /// </summary>
        public string Nonce {
            get; set;
        }
    }
}
