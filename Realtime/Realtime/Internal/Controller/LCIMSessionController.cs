using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Common;
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
            session.ConfigBitmap = 0xAB;
            session.Ua = $"{LCCore.SDKName}/{LCCore.SDKVersion}";
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
                Connection.Register(Client);
            } else if (response.Op == OpType.Closed) {
                Connection.UnRegister(Client);
                SessionCommand command = response.SessionMessage;
                throw new LCException(command.Code, command.Reason);
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
                Dictionary<string, object> ret = await LCCore.HttpClient.Post<Dictionary<string, object>>("rtm/sign", data: new Dictionary<string, object> {
                    { "session_token", Client.SessionToken }
                });
                signature = new LCIMSignature {
                    Signature = ret["signature"] as string,
                    Timestamp = (long)ret["timestamp"],
                    Nonce = ret["nonce"] as string
                };
            }
            if (signature != null && signature.IsValid) {
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
                case OpType.Closed: {
                        Connection.UnRegister(Client);
                        SessionCommand command = notification.SessionMessage;
                        Client.OnClose(command.Code, command.Reason);
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
