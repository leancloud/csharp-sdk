namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMBinaryMessage is a local representation of binary message in LeanCloud.
    /// </summary>
    public class LCIMBinaryMessage : LCIMMessage {
        public byte[] Data {
            get; internal set;
        }

        public LCIMBinaryMessage(byte[] data) {
            Data = data;
        }

        internal static LCIMBinaryMessage Deserialize(byte[] bytes) {
            return new LCIMBinaryMessage(bytes);
        }
    }
}
