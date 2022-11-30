using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace LeanCloud.Play {
    public class RoomSysPropsTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator RoomOpen() {
            var flag = false;
            var c = Utils.NewClient("rsp0");
            Room room = null;
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(t => {
                room = t.Result;
                c.OnRoomSystemPropertiesChanged += changedProps => {
                    var openObj = changedProps["open"];
                    var open = bool.Parse(openObj.ToString());
                    Assert.AreEqual(open, false);
                    Assert.AreEqual(room.Open, false);
                    flag = true;
                };  
                _ = room.SetOpen(false);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!flag) {
                yield return null;
            }
            _ = c.Close();
        }

        [UnityTest]
        public IEnumerator RoomVisible() {
            var flag = false;
            var c = Utils.NewClient("rsp1");
            Room room = null;
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(t => {
                room = t.Result;
                c.OnRoomSystemPropertiesChanged += changedProps => {
                    var visibleObj = changedProps["visible"];
                    var visible = bool.Parse(visibleObj.ToString());
                    Assert.AreEqual(visible, false);
                    Assert.AreEqual(room.Visible, false);
                    flag = true;
                };
                _ = room.SetVisible(false);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!flag) {
                yield return null;
            }
            _ = c.Close();
        }

        [UnityTest]
        public IEnumerator RoomMaxPlayerCount() {
            var flag = false;
            var c = Utils.NewClient("rsp2");
            Room room = null;
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(t => {
                room = t.Result;
                c.OnRoomSystemPropertiesChanged += changedProps => {
                    var maxPlayerCountObj = changedProps["maxPlayerCount"];
                    var maxPlayerCount = int.Parse(maxPlayerCountObj.ToString());
                    Assert.AreEqual(maxPlayerCount, 5);
                    Assert.AreEqual(room.MaxPlayerCount, 5);
                    flag = true;
                };
                _ = room.SetMaxPlayerCount(5);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!flag) {
                yield return null;
            }
            _ = c.Close();
        }

        [UnityTest]
        public IEnumerator RoomSetAndClearExpectedUserIds() {
            var f1 = false;
            var f2 = false;
            var c = Utils.NewClient("rsp3");
            Room room = null;
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(t => {
                room = t.Result;
                c.OnRoomSystemPropertiesChanged += changedProps => {
                    var expectedUserIds = changedProps["expectedUserIds"] as List<string>;
                    f1 |= expectedUserIds.Count == 2 && room.ExpectedUserIds.Count == 2;
                    f2 |= expectedUserIds.Count == 0 && room.ExpectedUserIds.Count == 0;
                };
                return room.SetExpectedUserIds(new List<string> { "hello", "world" });
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Assert.AreEqual(room.ExpectedUserIds.Count, 2);
                return room.ClearExpectedUserIds();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Assert.AreEqual(room.ExpectedUserIds.Count, 0);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f1 || !f2) {
                yield return null;
            }
            _ = c.Close();
        }

        [UnityTest]
        public IEnumerator RoomAddAndRemoveExpectedUserIds() {
            var f1 = false;
            var f2 = false;
            var f3 = false;

            var c = Utils.NewClient("rsp4");
            Room room = null;
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(t => {
                room = t.Result;
                c.OnRoomSystemPropertiesChanged += changedProps => {
                    List<string> expectedUserIds = changedProps["expectedUserIds"] as List<string>;
                    f1 |= expectedUserIds.Count == 1 && room.ExpectedUserIds.Count == 1;
                    f2 |= expectedUserIds.Count == 3 && room.ExpectedUserIds.Count == 3;
                    f3 |= expectedUserIds.Count == 2 && room.ExpectedUserIds.Count == 2;
                };
                return room.SetExpectedUserIds(new List<string> { "hello" });
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Assert.AreEqual(room.ExpectedUserIds.Count, 1);
                return room.AddExpectedUserIds(new List<string> { "csharp", "js" });
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Assert.AreEqual(room.ExpectedUserIds.Count, 3);
                return room.RemoveExpectedUserIds(new List<string> { "hello" });
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Assert.AreEqual(room.ExpectedUserIds.Count, 2);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f1 || !f2 || !f3) {
                yield return null;
            }
            _ = c.Close();
        }
    }
}
