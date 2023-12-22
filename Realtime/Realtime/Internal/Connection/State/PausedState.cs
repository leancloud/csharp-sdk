﻿
namespace LeanCloud.Realtime.Internal.Connection.State {
    public class PausedState : BaseState {
        public PausedState(LCConnection connection) : base(connection) {
        }

        #region State Event

        public override void Resume() {
            connection.TransitTo(LCConnection.State.Reconnect);
        }

        #endregion

    }
}