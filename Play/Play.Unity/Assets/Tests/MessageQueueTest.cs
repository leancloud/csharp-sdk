using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;

namespace LeanCloud.Play {
    public class MessageQueueTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator PauseAndResume() {
            var f0 = false;
            var f1 = false;

            var roomName = "mq0_r";
            var c0 = Utils.NewClient("mq0_0");
            var c1 = Utils.NewClient("mq1_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c0.OnPlayerRoomJoined += newPlayer => {
                    Debug.Log("---- new player joined");
                    Assert.AreEqual(newPlayer.UserId, "mq1_1");
                    f0 = true;
                };
                c0.OnCustomEvent += (eventId, eventData, sender) => {
                    Debug.Log("---- received event: " + eventId);
                    Assert.AreEqual(eventId, 4);
                    f1 = true;
                };
                c0.PauseMessageQueue();
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => Task.Delay(3000), TaskScheduler.FromCurrentSynchronizationContext())
            .Unwrap().OnSuccess(_ => {
                c0.ResumeMessageQueue();
                Debug.Log("resume message queue");
                c0.PauseMessageQueue();
                return c1.SendEvent(4);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => Task.Delay(5000), TaskScheduler.FromCurrentSynchronizationContext())
            .Unwrap().OnSuccess(_ => {
                Debug.Log("delay done");
                c0.ResumeMessageQueue();
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f0 || !f1) {
                yield return null;
            }
            _ = c0.Close();
            _ = c1.Close();
        }
    }
}
