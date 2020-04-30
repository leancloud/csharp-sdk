namespace LeanCloud.Realtime {
    /// <summary>
    /// 二进制消息
    /// </summary>
    public class LCIMBinaryMessage : LCIMMessage {
        /// <summary>
        /// 消息数据
        /// </summary>
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
