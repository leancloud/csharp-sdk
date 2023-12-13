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

        public enum State {
            Init,
            Connected,
            Reconnect,
            Paused
        }

        private BaseState currentState;

        internal LCConnection(string id) {
            this.id = id;

            router = new LCRTMRouter();
            
            idToClients = new Dictionary<string, LCIMClient>();

            currentState = new InitState(this);
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

        internal void TransitTo(State state) {
            LCLogger.Debug($"RTM Connection transits from {currentState.GetType().Name} to {state}");
            currentState.Exit();
            switch (state) {
                case State.Init:
                    currentState = new InitState(this);
                    break;
                case State.Connected:
                    currentState = new ConnectedState(this);
                    break;
                case State.Reconnect:
                    currentState = new ReconnectState(this);
                    break;
                case State.Paused:
                    currentState = new PausedState(this);
                    break;
            }
            currentState.Enter();
        }

        internal void HandleDisconnected() {
            foreach (LCIMClient client in idToClients.Values) {
                client.HandleDisconnected();
            }
        }

        internal void HandleReconnected() {
            foreach (LCIMClient client in idToClients.Values) {
                client.HandleReconnected();
            }
        }
    }
}
