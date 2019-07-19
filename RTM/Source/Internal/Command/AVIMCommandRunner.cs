using LeanCloud;
using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    public class AVIMCommandRunner : IAVIMCommandRunner
    {
        private readonly IWebSocketClient webSocketClient;
        public AVIMCommandRunner(IWebSocketClient webSocketClient)
        {
            this.webSocketClient = webSocketClient;
        }

        public void RunCommand(AVIMCommand command)
        {
            command.IDlize();
            var requestString = command.EncodeJsonString();
            AVRealtime.PrintLog("websocket=>" + requestString);
            webSocketClient.Send(requestString);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Tuple<int, IDictionary<string, object>>> RunCommandAsync(AVIMCommand command, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<Tuple<int, IDictionary<string, object>>>();

            command.IDlize();

            var requestString = command.EncodeJsonString();
            if (!command.IsValid)
            {
                requestString = "{}";
            }
            AVRealtime.PrintLog("websocket=>" + requestString);
            webSocketClient.Send(requestString);
            var requestJson = command.Encode();


            Action<string> onMessage = null;
            onMessage = (response) =>
            {
                //AVRealtime.PrintLog("response<=" + response);
                var responseJson = Json.Parse(response) as IDictionary<string, object>;
                if (responseJson.Keys.Contains("i"))
                {
                    if (requestJson["i"].ToString() == responseJson["i"].ToString())
                    {
                        var result = new Tuple<int, IDictionary<string, object>>(-1, responseJson);
                        if (responseJson.Keys.Contains("code"))
                        {
                            var errorCode = int.Parse(responseJson["code"].ToString());
                            var reason = string.Empty;
                            int appCode = 0;

                            if (responseJson.Keys.Contains("reason"))
                            {
                                reason = responseJson["reason"].ToString();
                            }
                            if (responseJson.Keys.Contains("appCode"))
                            {
                                appCode = int.Parse(responseJson["appCode"].ToString());
                            }
                            tcs.SetException(new AVIMException(errorCode, appCode, reason, null));
                        }
                        if (tcs.Task.Exception == null)
                        {
                            tcs.SetResult(result);
                        }
                        webSocketClient.OnMessage -= onMessage;
                    }
                    else
                    {

                    }
                }
            };
            webSocketClient.OnMessage += onMessage;
            return tcs.Task;
        }
    }
}
