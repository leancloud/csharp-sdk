using System;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Realtime;
using UnityEngine;

public class NetworkListener : MonoBehaviour {
    private NetworkReachability lastReachability;

    void Start() {
        Debug.Log($"********************* {Application.internetReachability}");
        lastReachability = Application.internetReachability;
    }

    void Update() {
        if (Application.internetReachability != lastReachability) {
            Debug.Log($"********************* {Application.internetReachability}");
            // 切换网络，则先断网
            LCRealtime.Pause();
            if (Application.internetReachability != NetworkReachability.NotReachable) {
                // 如果有网，再开启恢复
                LCRealtime.Resume();
            }
            lastReachability = Application.internetReachability;
        }
    }

    private void OnApplicationPause(bool pauseStatus) {
        if (pauseStatus) {
            LCRealtime.Pause();
        } else {
            LCRealtime.Resume();
        }
    }
}
