namespace LeanCloud.Realtime {
    /// <summary>
    /// 支持签名的动作
    /// </summary>
    public static class LCIMSignatureAction {
        /// <summary>
        /// 邀请
        /// </summary>
        public const string Invite = "invite";

        /// <summary>
        /// 踢出
        /// </summary>
        public const string Kick = "kick";

        /// <summary>
        /// 用户拉黑对话
        /// </summary>
        public const string ClientBlockConversations = "client-block-conversations";

        /// <summary>
        /// 用户解除拉黑对话
        /// </summary>
        public const string ClientUnblockConversations = "client-unblock-conversations";

        /// <summary>
        /// 对话拉黑用户
        /// </summary>
        public const string ConversationBlockClients = "conversation-block-clients";

        /// <summary>
        /// 对话解除拉黑用户
        /// </summary>
        public const string ConversationUnblockClients = "conversation-unblock-clients";
    }
}
