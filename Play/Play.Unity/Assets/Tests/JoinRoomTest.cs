using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using System.Reflection;
using UnityEngine;

namespace LeanCloud.Play {
    public class JoinRoomTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        [Order(0)]
        public IEnumerator JoinRoomByName() {
            var f = false;
            var roomName = "jrt0_r";
            var c0 = Utils.NewClient("jrt0_0");
            var c1 = Utils.NewClient("jrt0_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(async _ => {
                var room = _.Result;
                Assert.AreEqual(room.Name, roomName);
                await c0.Close();
                await c1.Close();
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        [Order(1)]
        public IEnumerator JoinRandomRoom() {
            var f = false;
            var c0 = Utils.NewClient("jrt1_0");
            var c1 = Utils.NewClient("jrt1_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRandomRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(async _ => {
                var room = _.Result;
                Debug.Log($"join random: {room.Name}");
                await c0.Close();
                await c1.Close();
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        [Order(2)]
        public IEnumerator JoinWithExpectedUserIds() {
            var f = false;
            var roomName = "jrt2_r";
            var c0 = Utils.NewClient("jrt2_0");
            var c1 = Utils.NewClient("jrt2_1");
            var c2 = Utils.NewClient("jrt2_2");

            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    MaxPlayerCount = 2
                };
                return c0.CreateRoom(roomName, roomOptions, new List<string> { "jrt2_2" });
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().ContinueWith(t => {
                Assert.AreEqual(t.IsFaulted, true);
                PlayException exception = t.Exception.InnerException as PlayException;
                Assert.AreEqual(exception.Code, 4302);
                Debug.Log(exception.Detail);
                return c2.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c2.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(async t => {
                Room room = t.Result;
                Assert.AreEqual(room.Name, roomName);
                await c0.Close();
                await c1.Close();
                await c2.Close();
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        [Order(3)]
        public IEnumerator JoinWithExpectedUserIdsFixBug() {
            var f = false;
            var roomName = "jr9_r0";
            var c0 = Utils.NewClient("jr9_0");
            var c1 = Utils.NewClient("jr9_1");
            var c2 = Utils.NewClient("jr9_2");
            var c3 = Utils.NewClient("jr9_3");

            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    MaxPlayerCount = 4
                };
                return c0.CreateRoom(roomName, roomOptions, new List<string> { "jr9_1" });
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c2.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c2.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c3.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c3.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(async _ => {
                await c0.Close();
                await c1.Close();
                await c2.Close();
                await c3.Close();
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        [Order(4)]
        public IEnumerator MatchRandom() {
            var f = false;

            var roomName = "jr8_r";
            var c0 = Utils.NewClient("jr8_0");
            var c1 = Utils.NewClient("jr8_1");
            var c2 = Utils.NewClient("jr8_2");
            var c3 = Utils.NewClient("jr8_xxx");

            var props = new PlayObject {
                { "lv", 5 }
            };
            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    MaxPlayerCount = 3,
                    CustomRoomProperties = props,
                    CustomRoomPropertyKeysForLobby = new List<string> { "lv" }
                };
                return c0.CreateRoom(roomName, roomOptions);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log("c1 connected");
                return c1.MatchRandom("jr8_1", new PlayObject {
                    { "lv", 5 }
                }, new List<string> { "jr8_xxx" });
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(t => {
                var roomId = t.Result;
                Assert.AreEqual(roomId, roomName);
                return c1.JoinRoom(roomId);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c2.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c2.JoinRandomRoom(props);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().ContinueWith(t => {
                PlayException e = (PlayException)t.Exception.InnerException;
                Assert.AreEqual(e.Code, 4301);
                _ = c2.Close();
                return c3.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c3.JoinRandomRoom(props);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(async _ => {
                await c0.Close();
                await c1.Close();
                await c3.Close();
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        [Order(5)]
        public IEnumerator LeaveRoom() {
            var f0 = false;
            var f1 = false;
            var roomName = "jrt3_r";
            var c0 = Utils.NewClient("jrt3_0");
            var c1 = Utils.NewClient("jrt3_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c0.OnPlayerRoomLeft += leftPlayer => {
                    Debug.Log("left");
                    f0 = true;
                };
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log($"{c1.UserId} joined room");
                return c1.LeaveRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(async _ => {
                await c0.Close();
                await c1.Close();
                f1 = true;
                Debug.Log("left");
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f0 || !f1) {
                yield return null;
            }

        }

        [UnityTest]
        public IEnumerator RejoinRoom() {
            var f = false;
            var roomName = $"jrt4_r_{Random.Range(0, 1000000)}";
            var c0 = Utils.NewClient("jrt4_0");
            var c1 = Utils.NewClient("jrt4_1");

            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    PlayerTtl = 600
                };
                return c0.CreateRoom(roomName, roomOptions);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c1.OnDisconnected += () => {
                    Debug.Log("------------- disconnected");
                    c1.Connect().OnSuccess(async _ => {
                        string lastRoomName = await c1.FetchMyRoom();
                        return c1.RejoinRoom(lastRoomName);
                    }).Unwrap().Unwrap().OnSuccess(async __ => {
                        await c0.Close();
                        await c1.Close();
                        f = true;
                    });
                };
                DisconnectRoom(c1.Room);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        [Timeout(20 * 1000)]
        public IEnumerator ReconnectAndRejoin() {
            var f = false;
            var roomName = "jrt5_r";
            var c0 = Utils.NewClient("jrt5_0");
            var c1 = Utils.NewClient("jrt5_1");

            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    PlayerTtl = 600
                };
                return c0.CreateRoom(roomName, roomOptions);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c1.OnDisconnected += () => {
                    c1.ReconnectAndRejoin().OnSuccess(async __ => {
                        await c0.Close();
                        await c1.Close();
                        f = true;
                    });
                };
                DisconnectRoom(c1.Room);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator JoinRoomFailed() {
            var f = false;
            var roomName = "jrt6_r";
            var c = Utils.NewClient("jrt6");

            c.Connect().OnSuccess(_ => {
                return c.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().ContinueWith(async _ => {
                Assert.AreEqual(_.IsFaulted, true);
                var e = _.Exception.InnerException as PlayException;
                Assert.AreEqual(e.Code, 4301);
                Debug.Log(e.Detail);
                await c.Close();
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        [Timeout(20 * 1000)]
        public IEnumerator JoinRandomWithMatchProperties() {
            var f = false;
            var roomName = "jrt7_r";
            var c0 = Utils.NewClient("jrt7_0");
            var c1 = Utils.NewClient("jrt7_1");
            var c2 = Utils.NewClient("jrt7_2");
            var c3 = Utils.NewClient("jrt7_3");
            var c4 = Utils.NewClient("jrt7_2");

            var props = new PlayObject {
                { "lv", 2 }
            };
            c0.Connect().OnSuccess(_ => {
                var roomOptions = new RoomOptions {
                    MaxPlayerCount = 3,
                    CustomRoomProperties = props,
                    CustomRoomPropertyKeysForLobby = new List<string> { "lv" }
                };
                return c0.CreateRoom(roomName, roomOptions);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRandomRoom(props, new List<string> { "jrt7_2" });
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c2.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c2.JoinRandomRoom(new PlayObject {
                    { "lv", 3 }
                });
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().ContinueWith(async t => {
                PlayException e = (PlayException)t.Exception.InnerException;
                Assert.AreEqual(e.Code, 4301);
                await c2.Close();
                return await c3.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c3.JoinRandomRoom(props);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().ContinueWith(async t => {
                PlayException e = (PlayException)t.Exception.InnerException;
                Assert.AreEqual(e.Code, 4301);
                await c3.Close();
                return await c4.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c4.JoinRandomRoom(props);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(async _ => {
                await c0.Close();
                await c1.Close();
                await c4.Close();
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CreateAfterJoinFailed() {
            var f = false;
            var roomName = "jrt8_r";
            var c0 = Utils.NewClient("jrt8_0");
            c0.Connect().OnSuccess(_ => {
                return c0.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().ContinueWith(t => {
                Assert.AreEqual(t.IsFaulted, true);
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(async t => {
                await c0.Close();
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        private static void DisconnectRoom(Room room) {
            FieldInfo connProp = typeof(Room).GetField("gameConn", BindingFlags.Instance | BindingFlags.NonPublic);
            object conn = connProp.GetValue(room);
            MethodInfo method = conn.GetType().BaseType.GetMethod("OnDisconnect", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(conn, null);
        }
    }
}
