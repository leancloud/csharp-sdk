using System;
using System.Threading.Tasks;
using LeanCloud.Realtime.Protocol;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMSessionController : LCIMController {
        private string token;
        private DateTimeOffset expiredAt;

        internal LCIMSessionController(LCIMClient client) : base(client) {

        }

        internal async Task Open() {
            SessionCommand session = NewSessionCommand();
            GenericCommand request = Client.NewCommand(CommandType.Session, OpType.Open);
            request.SessionMessage = session;
            GenericCommand response = await Client.Connection.SendRequest(request);
            UpdateSession(response.SessionMessage);
        }

        internal async Task Close() {
            GenericCommand request = Client.NewCommand(CommandType.Session, OpType.Close);
            await Client.Connection.SendRequest(request);
        }

        internal async Task<string> GetToken() {
            if (IsExpired) {
                await Refresh();
            }
            return token;
        }

        private async Task Refresh() {
            SessionCommand session = NewSessionCommand();
            GenericCommand request = Client.NewCommand(CommandType.Session, OpType.Refresh);
            request.SessionMessage = session;
            GenericCommand response = await Client.Connection.SendRequest(request);
            UpdateSession(response.SessionMessage);
        }

        private SessionCommand NewSessionCommand() {
            SessionCommand session = new SessionCommand();
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

        internal override async Task OnNotification(GenericCommand notification) {
            switch (notification.Op) {
                case OpType.Closed:
                    await OnClosed(notification.SessionMessage);
                    break;
                default:
                    break;
            }
        }

        private bool IsExpired {
            get {
                return DateTimeOffset.Now > expiredAt;
            }
        }

        private async Task OnClosed(SessionCommand session) {
            int code = session.Code;
            string reason = session.Reason;
            string detail = session.Detail;
            await Connection.Close();
            Client.OnClose?.Invoke(code, reason, detail);
        }
    }
}
