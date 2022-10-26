namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMSignature represents a LCRealtime signature.
    /// </summary>
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

        public bool IsValid =>
            !string.IsNullOrEmpty(Signature) &&
            !string.IsNullOrEmpty(Nonce) &&
            Timestamp > 0;
    }
}
