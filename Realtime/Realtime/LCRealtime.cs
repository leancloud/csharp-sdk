using System.Collections.Generic;
using LeanCloud.Realtime.Internal.Connection;

namespace LeanCloud.Realtime {
    public class LCRealtime {
        /// <summary>
        /// RTM 服务中，每个 app 对应一条连接
        /// </summary>
        private static readonly Dictionary<string, LCConnection> appToConnections = new Dictionary<string, LCConnection>();

        /// <summary>
        /// 获取对应的 Connection
        /// </summary>
        /// <param name="appId"></param>
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
        /// 移除 Connection
        /// </summary>
        /// <param name="connection"></param>
        internal static void RemoveConnection(LCConnection connection) {
            appToConnections.Remove(connection.id);
        }

        /// <summary>
        /// 主动断开所有 RTM 连接
        /// </summary>
        public static void Pause() {
            foreach (LCConnection connection in appToConnections.Values) {
                connection.Pause();
            }
        }

        /// <summary>
        /// 主动恢复所有 RTM 连接
        /// </summary>
        public static void Resume() {
            foreach (LCConnection connection in appToConnections.Values) {
                connection.Resume();
            }
        }
    }
}
