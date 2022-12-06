using System.Collections.Generic;

namespace LeanCloud.Play {
    /// <summary>
    /// 接收组枚举
    /// </summary>
    public enum ReceiverGroup {
        /// <summary>
        /// 其他人（除了自己之外的所有人）
        /// </summary>
        Others = 0,
        /// <summary>
        /// 所有人（包括自己）
        /// </summary>
        All = 1,
        /// <summary>
        /// 主机客户端
        /// </summary>
        MasterClient = 2,
    }

    /// <summary>
    /// 发送事件选项
    /// </summary>
    public class SendEventOptions {
        /// <summary>
        /// 接收组
        /// </summary>
        /// <value>The receiver group.</value>
        public ReceiverGroup ReceiverGroup {
            get; set;
        }

        /// <summary>
        /// 接收者 Id。如果设置，将会覆盖 ReceiverGroup
        /// </summary>
        /// <value>The target actor identifiers.</value>
        public List<int> TargetActorIds {
            get; set;
        }
    }
}
