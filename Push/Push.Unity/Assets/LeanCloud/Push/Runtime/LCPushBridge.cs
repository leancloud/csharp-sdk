﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LC.Newtonsoft.Json;
using LeanCloud.Storage;
using LeanCloud.Common;

namespace LeanCloud.Push {
    public class LCPushBridge : MonoBehaviour {
        private const string PUSH_BRIDGE = "__LC_PUSH_BRIDGE__";

        private const string CALLBACK_ID = "callbackId";

        public static LCPushBridge Instance { get; private set; }

        public Action<Dictionary<string, object>> OnReceiveNotification { get; set; }

        private readonly Dictionary<string, Action<Dictionary<string, object>>> id2Callbacks = new Dictionary<string, Action<Dictionary<string, object>>>();

        [RuntimeInitializeOnLoadMethod]
        private static void OnLoad() {
            // 启动时创建不销毁的 GameObject 对象用于接收 Native 的消息
            GameObject go = new GameObject(PUSH_BRIDGE);
            go.AddComponent<LCPushBridge>();
            DontDestroyOnLoad(go);
        }

        private void Awake() {
            Instance = this;
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
                LCLogger.Error(e);
            }
        }

        /// <summary>
        /// 通知启动回调
        /// </summary>
        /// <param name="json"></param>
        public void OnGetLaunchData(string json) {
            if (string.IsNullOrEmpty(json)) {
                return;
            }

            try {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json,
                    LCJsonConverter.Default);
                if (data.TryGetValue(CALLBACK_ID, out object idObj) &&
                    idObj is string id &&
                    id2Callbacks.TryGetValue(id, out Action<Dictionary<string, object>> callback)) {
                    callback.Invoke(data);
                    id2Callbacks.Remove(id);
                }
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }

        /// <summary>
        /// 通知回调
        /// </summary>
        /// <param name="json"></param>
        public async void OnReceiveMessage(string json) {
            if (OnReceiveNotification == null) {
                // 如果没有注册通知回调，则不默认获取推送数据，避免因为项目侧延迟注册回调，导致推送数据丢失
                return;
            }

            Dictionary<string, object> launchData = await GetLaunchData();
            if (launchData == null || launchData.Count == 0) {
                return;
            }

            OnReceiveNotification.Invoke(launchData);
        }

        public Task<Dictionary<string, object>> GetLaunchData() {
            if (Application.platform == RuntimePlatform.IPhonePlayer) {
                TaskCompletionSource<Dictionary<string, object>> tcs = new TaskCompletionSource<Dictionary<string, object>>();

                string callbackId = Guid.NewGuid().ToString();
                id2Callbacks.Add(callbackId, data => {
                    data.Remove(CALLBACK_ID);
                    tcs.TrySetResult(data);
                });
                LCIOSPushManager.GetLaunchData(callbackId);

                return tcs.Task;
            } else if (Application.platform == RuntimePlatform.Android) {
                return Task.FromResult(LCAndroidPushManager.GetLaunchData());
            }

            return Task.FromResult<Dictionary<string, object>>(null);
        }
    }
}
