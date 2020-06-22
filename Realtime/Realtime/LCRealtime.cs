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
            string connId = appId.Substring(0, 8).ToLower();
            connection = new LCConnection(connId);
            appToConnections[appId] = connection;
            return connection;
        }

        /// <summary>
        /// 主动断开所有 RTM 连接
        /// </summary>
        public static void Pause() {

        }

        /// <summary>
        /// 主动恢复所有 RTM 连接
        /// </summary>
        public static void Resume() {

        }
    }
}
