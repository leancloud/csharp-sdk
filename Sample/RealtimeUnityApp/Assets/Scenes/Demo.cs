using System;
using System.Collections;
using System.Collections.ObjectModel;
using LeanCloud.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Demo : MonoBehaviour {
    public Text stateText;
    public Toggle networkListenerToggle;

    void Start() {
        stateText.text = "Connected";
        LCIMClient client = RTM.Instance.Client;
        client.OnPaused = () => {
            stateText.text = "Paused";
        };
        client.OnResume = () => {
            stateText.text = "Reconnected";
        };
        networkListenerToggle.onValueChanged.AddListener(OnNetworkListenerValueChanged);
        OnNetworkListenerValueChanged(networkListenerToggle.isOn);
    }

    public async void OnQueryConversationsClicked() {
        try {
            LCIMConversationQuery query = RTM.Instance.Client.GetQuery();
            ReadOnlyCollection<LCIMConversation> conversations = await query.Find();
            foreach (LCIMConversation conv in conversations) {
                Debug.Log(conv.Id);
            }
        } catch (Exception e) {
            Debug.LogError($"Query conversation exception: {e}");
        }
    }

    public void OnPauseClicked() {
        LCRealtime.Pause();
    }

    public void OnResumeClicked() {
        LCRealtime.Resume();
    }

    public void OnPauseResumeX5Clicked() {
        for (int i = 0; i < 5; i++) {
            LCRealtime.Pause(); 
            LCRealtime.Resume();
        }
    }

    public async void OnCloseClicked() {
        LCIMClient client = RTM.Instance.Client;
        try {
            await client.Close();
            SceneManager.LoadScene("Login");
        } catch (Exception e) {
            Debug.LogError($"Logout exception: {e}");
        }
    }

    public void OnNetworkListenerValueChanged(bool isOn) {
        if (isOn) {
            gameObject.AddComponent<NetworkListener>();
        } else {
            Destroy(gameObject.GetComponent<NetworkListener>());
        }
    }
}
