namespace LeanCloud.Realtime {
    /// <summary>
    /// 签名数据
    /// </summary>
    public class LCIMSignature {
        /// <summary>
        /// 签名
        /// </summary>
        public string Signature {
            get; set;
        }

        /// <summary>
        /// 时间戳
        /// </summary>
        public long Timestamp {
            get; set;
        }

        /// <summary>
        /// 随机字符串
        /// </summary>
        public string Nonce {
            get; set;
        }
    }
}
