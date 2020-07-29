using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal.Protocol;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMSessionController : LCIMController {
        private string token;
        private DateTimeOffset expiredAt;

        internal LCIMSessionController(LCIMClient client)
            : base(client) {

        }

        #region 内部接口


        internal async Task Open(bool force) {
            await Connection.Connect();

            SessionCommand session = await NewSessionCommand();
            session.R = !force;
            session.ConfigBitmap = 0x2B;
            GenericCommand request = NewCommand(CommandType.Session, OpType.Open);
            request.SessionMessage = session;
            GenericCommand response = await Connection.SendRequest(request);
            UpdateSession(response.SessionMessage);
            Connection.Register(Client);
        }

        internal async Task Reopen() {
            SessionCommand session = await NewSessionCommand();
            session.R = true;
            GenericCommand request = NewCommand(CommandType.Session, OpType.Open);
            request.SessionMessage = session;
            GenericCommand response = await Connection.SendRequest(request);
            if (response.Op == OpType.Opened) {
                UpdateSession(response.SessionMessage);
            } else if (response.Op == OpType.Closed) {
                OnClosed(response.SessionMessage);
            }
        }


        internal async Task Close() {
            GenericCommand request = NewCommand(CommandType.Session, OpType.Close);
            await Connection.SendRequest(request);
            Connection.UnRegister(Client);
        }

 
        internal async Task<string> GetToken() {
            if (IsExpired) {
                await Refresh();
            }
            return token;
        }

        #endregion

        private async Task Refresh() {
            SessionCommand session = await NewSessionCommand();
            GenericCommand request = NewCommand(CommandType.Session, OpType.Refresh);
            request.SessionMessage = session;
            GenericCommand response = await Connection.SendRequest(request);
            UpdateSession(response.SessionMessage);
        }

        private async Task<SessionCommand> NewSessionCommand() {
            SessionCommand session = new SessionCommand();
            if (Client.Tag != null) {
                session.Tag = Client.Tag;
            }
            if (Client.DeviceId != null) {
                session.DeviceId = Client.DeviceId;
            }
            LCIMSignature signature = null;
            if (Client.SignatureFactory != null) {
                signature = await Client.SignatureFactory.CreateConnectSignature(Client.Id);
            }
            if (signature == null && !string.IsNullOrEmpty(Client.SessionToken)) {
                Dictionary<string, object> ret = await LCApplication.HttpClient.Post<Dictionary<string, object>>("rtm/sign", data: new Dictionary<string, object> {
                    { "session_token", Client.SessionToken }
                });
                signature = new LCIMSignature {
                    Signature = ret["signature"] as string,
                    Timestamp = (long)ret["timestamp"],
                    Nonce = ret["nonce"] as string
                };
            }
            if (signature != null) {
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

        internal override void HandleNotification(GenericCommand notification) {
            switch (notification.Op) {
                case OpType.Closed:
                    OnClosed(notification.SessionMessage);
                    break;
                default:
                    break;
            }
        }

 
        private void OnClosed(SessionCommand session) {
            int code = session.Code;
            string reason = session.Reason;
            string detail = session.Detail;
            Connection.UnRegister(Client);
            Client.OnClose?.Invoke(code, reason);
        }

        #endregion
    }
}
