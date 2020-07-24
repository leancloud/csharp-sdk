using System.Collections.Generic;
using LeanCloud.Realtime.Internal.Connection;

namespace LeanCloud.Realtime {
    public class LCRealtime {
        /// <summary>
        /// Every application uses a connection.
        /// </summary>
        private static readonly Dictionary<string, LCConnection> appToConnections = new Dictionary<string, LCConnection>();

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="appId">App ID of the application</param>
        /// <returns></returns>
        internal static LCConnection GetConnection(string appId) {
            if (appToConnections.TryGetValue(appId, out LCConnection connection)) {
                return connection;
            }
            connection = new LCConnection(appId);
            appToConnections[appId] = connection;
            return connection;
        }

        /// <summary>
        /// Removes the connection.
        /// </summary>
        /// <param name="connection">The LCConnection to remove</param>
        internal static void RemoveConnection(LCConnection connection) {
            appToConnections.Remove(connection.id);
        }

        /// <summary>
        /// Disconnects all.
        /// </summary>
        public static void Pause() {
            foreach (LCConnection connection in appToConnections.Values) {
                connection.Pause();
            }
        }

        /// <summary>
        /// Reconnects all.
        /// </summary>
        public static void Resume() {
            foreach (LCConnection connection in appToConnections.Values) {
                connection.Resume();
            }
        }
    }
}
