using System;
using System.Threading.Tasks;
using LeanCloud.Realtime.Protocol;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMSessionController : LCIMController {
        private string token;
        private DateTimeOffset expiredAt;

        internal LCIMSessionController(LCIMClient client)
            : base(client) {

        }

        #region 内部接口

        /// <summary>
        /// 打开会话
        /// </summary>
        /// <returns></returns>
        internal async Task Open(bool reconnect) {
            SessionCommand session = NewSessionCommand();
            session.R = reconnect;
            GenericCommand request = NewCommand(CommandType.Session, OpType.Open);
            request.SessionMessage = session;
            GenericCommand response = await Client.Connection.SendRequest(request);
            UpdateSession(response.SessionMessage);
        }

        /// <summary>
        /// 重新打开会话，重连时调用
        /// </summary>
        /// <returns></returns>
        internal async Task Reopen() {
            SessionCommand session = NewSessionCommand();
            session.R = true;
            GenericCommand request = NewCommand(CommandType.Session, OpType.Open);
            request.SessionMessage = session;
            GenericCommand response = await Client.Connection.SendRequest(request);
            if (response.Op == OpType.Opened) {
                UpdateSession(response.SessionMessage);
            } else if (response.Op == OpType.Closed) {
                await OnClosed(response.SessionMessage);
            }
        }

        /// <summary>
        /// 关闭会话
        /// </summary>
        /// <returns></returns>
        internal async Task Close() {
            GenericCommand request = NewCommand(CommandType.Session, OpType.Close);
            await Client.Connection.SendRequest(request);
        }

        /// <summary>
        /// 获取可用 token
        /// </summary>
        /// <returns></returns>
        internal async Task<string> GetToken() {
            if (IsExpired) {
                await Refresh();
            }
            return token;
        }

        #endregion

        private async Task Refresh() {
            SessionCommand session = NewSessionCommand();
            GenericCommand request = NewCommand(CommandType.Session, OpType.Refresh);
            request.SessionMessage = session;
            GenericCommand response = await Client.Connection.SendRequest(request);
            UpdateSession(response.SessionMessage);
        }

        private SessionCommand NewSessionCommand() {
            SessionCommand session = new SessionCommand();
            if (Client.Tag != null) {
                session.Tag = Client.Tag;
                session.DeviceId = Guid.NewGuid().ToString();
            }
            if (Client.SignatureFactory != null) {
                LCIMSignature signature = Client.SignatureFactory.CreateConnectSignature(Client.Id);
                session.S = signature.Signature;
                session.T = signature.Timestamp;
                session.N = signature.Nonce;
            }
            return session;
        }

        private void UpdateSession(SessionCommand session) {
            token = session.St;
            int ttl = session.StTtl;
            expiredAt = DateTimeOffset.Now + TimeSpan.FromSeconds(ttl);
        }

        private bool IsExpired {
            get {
                return DateTimeOffset.Now > expiredAt;
            }
        }

        #region 消息处理

        internal override async Task OnNotification(GenericCommand notification) {
            switch (notification.Op) {
                case OpType.Closed:
                    await OnClosed(notification.SessionMessage);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 被关闭
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private async Task OnClosed(SessionCommand session) {
            int code = session.Code;
            string reason = session.Reason;
            string detail = session.Detail;
            await Connection.Close();
            Client.OnClose?.Invoke(code, reason);
        }

        #endregion
    }
}
