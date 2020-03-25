using System.Threading.Tasks;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Realtime.Internal.WebSocket;

namespace LeanCloud.Realtime.Internal.Controller {
    internal abstract class LCIMController {
        protected LCIMClient Client {
            get; set;
        }

        internal LCIMController(LCIMClient client) {
            Client = client;
        }

        internal abstract Task OnNotification(GenericCommand notification);

        protected LCWebSocketConnection Connection {
            get {
                return Client.Connection;
            }
        }
    }
}
