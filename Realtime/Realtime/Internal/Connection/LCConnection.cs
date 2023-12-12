using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal.Router;
using LeanCloud.Realtime.Internal.WebSocket;
using LeanCloud.Realtime.Internal.Protocol;
using LeanCloud.Realtime.Internal.Connection.State;

namespace LeanCloud.Realtime.Internal.Connection {
    /// <summary>
    /// Connection layer
    /// </summary>
    public class LCConnection {
        internal string id;

        internal LCRTMRouter router;

        internal LCWebSocketClient ws;

        // 共享这条连接的 IM Client
        internal readonly Dictionary<string, LCIMClient> idToClients;
        // 默认 Client id
        internal string defaultClientId;

        internal InitState initState;
        internal ConnectedState connectedState;
        internal ReconnectState reconnectState;
        internal PausedState pausedState;

        private BaseState currentState;

        internal LCConnection(string id) {
            this.id = id;

            router = new LCRTMRouter();
            
            idToClients = new Dictionary<string, LCIMClient>();

            initState = new InitState(this);
            connectedState = new ConnectedState(this);
            reconnectState = new ReconnectState(this);
            pausedState = new PausedState(this);

            currentState = initState;
        }

        internal void Register(LCIMClient client) {
            idToClients[client.Id] = client;
            if (defaultClientId == null) {
                defaultClientId = client.Id;
            }
        }

        internal void UnRegister(LCIMClient client) {
            idToClients.Remove(client.Id);
            if (idToClients.Count == 0) {
                currentState.Close();
                LCRealtime.RemoveConnection(this);
            }
        }

        internal Task Connect() {
            return currentState.Connect();
        }

        internal Task<GenericCommand> SendRequest(GenericCommand request) {
            return currentState.SendRequest(request);
        }

        internal Task SendCommand(GenericCommand command) {
            return currentState.SendCommand(command);
        }

        internal void Pause() {
            currentState.Pause();
        }

        internal void Resume() {
            currentState.Resume();
        }

        public void TranslateTo(BaseState state) {
            if (currentState != null) {
                currentState.Exit();
            }
            currentState = state;
            currentState.Enter();
        }

        public void HandleDisconnected() {
            foreach (LCIMClient client in idToClients.Values) {
                client.HandleDisconnected();
            }
        }

        public void HandleReconnected() {
            foreach (LCIMClient client in idToClients.Values) {
                client.HandleReconnected();
            }
        }
    }
}
