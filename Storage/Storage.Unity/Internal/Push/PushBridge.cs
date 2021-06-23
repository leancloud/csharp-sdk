using System;
using System.Collections.Generic;
using UnityEngine;
using LC.Newtonsoft.Json;

namespace LeanCloud.Storage.Internal.Push {
    public class PushBridge : MonoBehaviour {
        private const string PUSH_BRIDGE = "__LC_PUSH_BRIDGE__";

        [RuntimeInitializeOnLoadMethod]
        private static void OnLoad() {
            // 启动时创建不销毁的 GameObject 对象用于接收 Native 的消息
            GameObject go = new GameObject(PUSH_BRIDGE);
            go.AddComponent<PushBridge>();
            DontDestroyOnLoad(go);
        }

        /// <summary>
        /// 注册设备推送的统一回调
        /// </summary>
        /// <param name="json">设备信息</param>
        public async void OnRegisterPush(string json) {
            if (string.IsNullOrEmpty(json)) {
                return;
            }

            try {
                Dictionary<string, object> deviceInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                LCInstallation installation = await LCInstallation.GetCurrent();
                foreach (KeyValuePair<string, object> info in deviceInfo) {
                    installation[info.Key] = info.Value;
                }
                await installation.Save();
            } catch (Exception e) {
                LCLogger.Error(e.ToString());
            }
        }
    }
}
