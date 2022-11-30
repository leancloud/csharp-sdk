using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using System.Threading;

namespace LeanCloud.Play {
    public class ConnectTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator Connect() {
            var f = false;
            var c = Utils.NewClient("ct0");
            c.Connect().OnSuccess(async _ => {
                Debug.Log($"{c.UserId} connected.");
                await c.Close();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().ContinueWith(_ => {
                f = true;
            });

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CloseFromLobby() {
            var f = false;
            var c = Utils.NewClient("ct2");
            c.Connect().ContinueWith(async _ => {
                Assert.AreEqual(_.IsFaulted, false);
                await c.Close();
                c = Utils.NewClient("ct2");
                return c.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().Unwrap().OnSuccess(async _ => {
                await c.Close();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().ContinueWith(_ => {
                f = true;
            });

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CloseFromGame() {
            var f = false;
            var c = Utils.NewClient("ct3");
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().ContinueWith(_ => {
                _ = c.Close();
                Assert.AreEqual(_.IsFaulted, false);
                c = Utils.NewClient("ct3");
                return c.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                _ = c.Close();
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ConnectFailed() {
            var f = false;
            var c = Utils.NewClient("ct 4");
            c.Connect().ContinueWith(_ => { 
                Assert.AreEqual(_.IsFaulted, true);
                var e = _.Exception.InnerException as PlayException;
                Assert.AreEqual(e.Code, 4104);
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest, Timeout(40000)]
        public IEnumerator KeepAlive() {
            var f = false;
            var roomName = "ct5_r";
            var c = Utils.NewClient("ct5");

            c.Connect().OnSuccess(_ => {
                return c.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Task.Delay(30000).OnSuccess(async __ => {
                    Debug.Log("delay 30s done");
                    await c.Close();
                    f = true;
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
            
        }

        [UnityTest, Timeout(40000)]
        public IEnumerator SendOnly() {
            var f = false;
            var c = Utils.NewClient("ct6");
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Task.Run(async () => {
                    var count = 6;
                    while (count > 0 && !f) {
                        var options = new SendEventOptions { 
                            ReceiverGroup = ReceiverGroup.Others
                        };
                        await c.SendEvent(5, null, options);
                        Thread.Sleep(5000);
                    }
                });
                Task.Delay(30000).OnSuccess(async __ => {
                    Debug.Log("delay 30s done");
                    await c.Close();
                    f = true;
                });
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
            
        }

        [UnityTest]
        public IEnumerator ConnectRepeatedly() {
            var f = false;
            var c = Utils.NewClient("ct7");

            c.Connect().OnSuccess(_ => {
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());
            _ = c.Connect();

            while (!f) {
                yield return null;
            }
            _ = c.Close();
        }
    }
}
