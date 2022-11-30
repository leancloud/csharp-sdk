using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;

namespace LeanCloud.Play {
    public class LobbyTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Log;
        }

        [UnityTest, Timeout(100000)]
        public IEnumerator RoomListUpdate() {
            var f = false;
            var c0 = Utils.NewClient("lt0_0");
            var c1 = Utils.NewClient("lt0_1");
            var c2 = Utils.NewClient("lt0_2");
            var c3 = Utils.NewClient("lt0_3");

            c0.Connect().OnSuccess(_ => {
                c0.OnLobbyRoomListUpdated += roomList => {
                    Debug.Log($"the count of rooms is {roomList.Count}");
                    f = roomList.Count >= 3;
                };
                return c0.JoinLobby();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                RoomOptions options = new RoomOptions {
                    CustomRoomProperties = new PlayObject {
                        { "owner", "123" }
                    },
                    CustomRoomPropertyKeysForLobby = new System.Collections.Generic.List<string> { "owner" }
                };
                return c1.CreateRoom(roomOptions: options);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c2.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c2.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c3.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c3.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log("create dones");
            }, TaskScheduler.FromCurrentSynchronizationContext());
                
            while (!f) {
                yield return null;
            }
            _ = c0.Close();
            _ = c1.Close();
            _ = c2.Close();
            _ = c3.Close();
        }
    }
}
