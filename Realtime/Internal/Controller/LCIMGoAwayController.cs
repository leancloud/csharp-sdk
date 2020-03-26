using System.Threading.Tasks;
using LeanCloud.Realtime.Protocol;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMGoAwayController : LCIMController {
        internal LCIMGoAwayController(LCIMClient client) : base(client) {

        }

        internal override async Task OnNotification(GenericCommand notification) {
            // 清空缓存，断开连接，等待重新连接
            Connection.Router.Reset();
            await Connection.Close();
        }
    }
}
