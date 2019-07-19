using System;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Realtime.Internal;
using LeanCloud.Storage.Internal;
using UnityEngine;
using UnityEngine.Networking;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// AVRealtime initialize behavior.
    /// </summary>
    public class AVRealtimeBehavior : AVInitializeBehaviour
    {
        public string RTMRouter = null;

        //void OnApplicationQuit()
        //{
        //    if (AVRealtime.clients != null)
        //    {
        //        foreach (var item in AVRealtime.clients)
        //        {
        //            item.Value.LinkedRealtime.LogOut();
        //        }
        //    }
        //}

        //private void Update()
        //{
        //    var available = Application.internetReachability != NetworkReachability.NotReachable;
        //    if (AVRealtime.clients != null)
        //        foreach (var item in AVRealtime.clients)
        //        {
        //            if (item.Value != null)
        //                if (item.Value.LinkedRealtime != null)
        //                    item.Value.LinkedRealtime.InvokeNetworkState(available);
        //        }
        //}

        //public override void Awake()
        //{
        //    base.Awake();
        //    StartCoroutine(InitializeRealtime());
        //    gameObject.name = "AVRealtimeInitializeBehavior";
        //}

        //public IEnumerator InitializeRealtime()
        //{
        //    if (isRealtimeInitialized)
        //    {
        //        yield break;
        //    }
        //    isRealtimeInitialized = true;
        //    yield return FetchRouter();
        //}


        //[SerializeField]
        //public bool secure;
        //private static bool isRealtimeInitialized = false;
        //public string Server;
        //private IDictionary<string, object> routerState;

        //public IEnumerator FetchRouter()
        //{
        //    var router = RTMRouter;
        //    if (string.IsNullOrEmpty(router)) {
        //        var state = AVPlugins.Instance.AppRouterController.Get();
        //        router = state.RealtimeRouterServer;
        //    }
        //    var url = string.Format("https://{0}/v1/route?appId={1}", router, applicationID);
        //    if (secure)
        //    {
        //        url += "&secure=1";
        //    }

        //    var request = new UnityWebRequest(url);
        //    request.downloadHandler = new DownloadHandlerBuffer();
        //    yield return request.Send();

        //    if (request.isError)
        //    {
        //        throw new AVException(AVException.ErrorCode.ConnectionFailed, "can not reach router.", null);
        //    }

        //    var result = request.downloadHandler.text;
        //    routerState = Json.Parse(result) as IDictionary<string, object>;
        //    if (routerState.Keys.Count == 0)
        //    {
        //        throw new KeyNotFoundException("Can not get websocket url from server,please check the appId.");
        //    }
        //    var ttl = long.Parse(routerState["ttl"].ToString());
        //    var expire = DateTime.Now.AddSeconds(ttl);
        //    routerState["expire"] = expire.ToUnixTimeStamp(UnixTimeStampUnit.Second);
        //    Server = routerState["server"].ToString();
        //}


    }
}
