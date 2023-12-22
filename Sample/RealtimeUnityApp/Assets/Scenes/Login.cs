using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using LeanCloud;
using LeanCloud.Realtime;

public class Login : MonoBehaviour {
    private static readonly string LC_APP_ID = "USkTbuVKNVE9ypnp4v96rclf-gzGzoHsz";
    
    private static readonly string LC_APP_KEY = "Gc9kgL97uWK3j3soCZr11WX9";

    private static readonly string LC_SERVER_URL = "https://usktbuvk.lc-cn-n1-shared.com";


    private static readonly string USER_ID_KEY = "USER_ID_KEY";

    public InputField userIdInput;

    void Start() {
        LCLogger.LogDelegate = (level, message) => {
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            switch (level) {
                case LCLogLevel.Debug:
                    Debug.Log($"[DEBUG] {time} {message}");
                break;
                case LCLogLevel.Warn:
                    Debug.LogWarning($"[WARNING] {time} {message}");
                break;
                case LCLogLevel.Error:
                    Debug.LogError($"[ERROR] {time} {message}");
                break;
                default:
                break;
            }
        };
        LCApplication.Initialize(LC_APP_ID, LC_APP_KEY, LC_SERVER_URL);

        string userId = PlayerPrefs.GetString(USER_ID_KEY, null);
        if (!string.IsNullOrEmpty(userId)) {
            userIdInput.text = userId;
        }
    }

    public async void OnLoginClicked() {
        string userId = userIdInput.text;
        if (string.IsNullOrEmpty(userId)) {
            Debug.LogError("Please input user id.");
            return;
        }

        try {
            RTM.Instance.Client = new LCIMClient(userId);
            await RTM.Instance.Client.Open();

            PlayerPrefs.SetString(USER_ID_KEY, userId);
            SceneManager.LoadScene("Demo");
        } catch (Exception e) {
            Debug.LogError(e);
        }
    }
}
