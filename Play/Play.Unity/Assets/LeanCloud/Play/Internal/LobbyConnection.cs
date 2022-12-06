using System.Threading.Tasks;
using LeanCloud.Play.Protocol;

namespace LeanCloud.Play {
    internal class LobbyConnection : BaseConnection {
        internal LobbyConnection(string appId, string server, string gameVersion, string userId, string sessionToken)
            : base(appId, server, gameVersion, userId, sessionToken) {
        }

        internal async Task JoinLobby() {
            var request = NewRequest();
            await SendRequest(CommandType.Lobby, OpType.Add, request);
        }

        internal async Task LeaveLobby() {
            RequestMessage request = NewRequest();
            await SendRequest(CommandType.Lobby, OpType.Remove, request);
        }

        protected override int KeepAliveInterval => 20 * 1000;

        protected override string GetFastOpenUrl(string server, string appId, string gameVersion, string userId, string sessionToken) {
            return $"{server}/1/multiplayer/lobby/websocket?appId={appId}&sdkVersion={Config.SDKVersion}&protocolVersion={Config.ProtocolVersion}&gameVersion={gameVersion}&userId={userId}&sessionToken={sessionToken}";
        }

        protected override void HandleNotification(CommandType cmd, OpType op, Body body) {
            OnNotification?.Invoke(cmd, op, body);
        }
    }
}
