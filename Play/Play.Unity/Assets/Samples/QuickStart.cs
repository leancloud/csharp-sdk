using UnityEngine;
using UnityEngine.UI;
using LeanCloud;
using LeanCloud.Play;

public class QuickStart : MonoBehaviour
{
    const byte GAME_OVER_EVENT = 100;

    public Text idText = null;
    public Text scoreText = null;
    public Text resultText = null;

    // 获取客户端 SDK 实例
    private Client client;

    // Use this for initialization
    async void Start() {
        // 设置 SDK 日志委托
        LCLogger.LogDelegate = (level, log) => {
            if (level == LCLogLevel.Debug) {
                Debug.LogFormat("[DEBUG] {0}", log);
            } else if (level == LCLogLevel.Warn) {
                Debug.LogFormat("[WARN] {0}", log);
            } else if (level == LCLogLevel.Error) {
                Debug.LogFormat("[ERROR] {0}", log);
            }
        };

        // App Id
        var APP_ID = "g2b0X6OmlNy7e4QqVERbgRJR-gzGzoHsz";
        // App Key
        var APP_KEY = "CM91rNV8cPVHKraoFQaopMVT";
        // 域名
        var playServer = "https://g2b0x6om.lc-cn-n1-shared.com";
        // 这里使用随机数作为 userId
        var random = new System.Random();
        var randId = string.Format("{0}", random.Next(10000000));
        idText.text = string.Format("Id: {0}", randId);
        // 初始化
        client = new Client(APP_ID, APP_KEY, randId, playServer: playServer);
        await client.Connect();
        Debug.Log("connected");
        // 根据当前时间（时，分）生成房间名称
        var now = System.DateTime.Now;
        var roomName = string.Format("{0}_{1}", now.Hour, now.Minute);
        await client.JoinOrCreateRoom(roomName);
        Debug.Log("joined room");

        // 注册新玩家加入房间事件
        client.OnPlayerRoomJoined += (newPlayer) => {
            Debug.LogFormat("new player: {0}", newPlayer.UserId);
            if (client.Player.IsMaster) {
                // 获取房间内玩家列表
                var playerList = client.Room.PlayerList;
                for (int i = 0; i < playerList.Count; i++) {
                    var player = playerList[i];
                    var props = new PlayObject();
                    // 判断如果是房主，则设置 10 分，否则设置 5 分
                    if (player.IsMaster) {
                        props.Add("point", 10);
                    } else {
                        props.Add("point", 5);
                    }
                    player.SetCustomProperties(props);
                }
                var data = new PlayObject {
                    { "winnerId", client.Room.Master.ActorId }
                };
                var opts = new SendEventOptions {
                    ReceiverGroup = ReceiverGroup.All
                };
                client.SendEvent(GAME_OVER_EVENT, data, opts);
            }
        };
        // 注册「玩家属性变更」事件
        client.OnPlayerCustomPropertiesChanged += (player, changedProps) => {
            // 判断如果玩家是自己，则做 UI 显示
            if (player.IsLocal) {
                // 得到玩家的分数
                long point = player.CustomProperties.GetInt("point");
                Debug.LogFormat("{0} : {1}", player.UserId, point);
                scoreText.text = string.Format("Score: {0}", point);
            }
        };
        // 注册自定义事件
        client.OnCustomEvent += (eventId, eventData, senderId) => {
            if (eventId == GAME_OVER_EVENT) {
                // 得到胜利者 Id
                int winnerId = eventData.GetInt("winnerId");
                // 如果胜利者是自己，则显示胜利 UI；否则显示失败 UI
                if (client.Player.ActorId == winnerId) {
                    Debug.Log("win");
                    resultText.text = "Win";
                } else {
                    Debug.Log("lose");
                    resultText.text = "Lose";
                }
                //client.Close();
            }
        };
        // 断线事件
        client.OnDisconnected += () => {
            Debug.Log("Disconnected.");
        };
    }
}
