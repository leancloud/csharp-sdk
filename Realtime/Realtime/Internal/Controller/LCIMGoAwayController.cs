using System.Threading.Tasks;
using LeanCloud.Realtime.Internal.Protocol;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMGoAwayController : LCIMController {
        internal LCIMGoAwayController(LCIMClient client) : base(client) {

        }

        #region 消息处理

        internal override void HandleNotification(GenericCommand notification) {
            // 清空缓存，断开连接，等待重新连接
            _ = Connection.Reset();
        }

        #endregion
    }
}
