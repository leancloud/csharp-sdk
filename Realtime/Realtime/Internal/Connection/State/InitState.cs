using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal.Connection.State {
    public class InitState : BaseState {
        // 可以在 connecting 状态时拿到 Task，并在重连成功后继续操作
        private Task connectTask;

        public InitState(LCConnection connection) : base(connection) {
        }

        #region State Event

        public override async Task Connect() {
            if (connectTask != null) {
                await connectTask;
                return;
            }

            connectTask = ConnectInternal(default);
            await connectTask;
            connection.TransitTo(LCConnection.State.Connected);
        }

        #endregion

    }
}
