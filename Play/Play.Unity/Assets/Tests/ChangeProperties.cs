using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;

namespace LeanCloud.Play {
    public class ChangeProperties {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator ChangeRoomProperties() {
            var roomName = "cp0_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var f0 = false;
            var f1 = false;
            var c0 = Utils.NewClient("cp0_0");
            var c1 = Utils.NewClient("cp0_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c0.OnRoomCustomPropertiesChanged += changedProps => {
                    var props = c0.Room.CustomProperties;
                    Assert.AreEqual(props.GetString("name"), "leancloud");
                    Assert.AreEqual(props.GetInt("gold"), 1000);
                    f0 = true;
                };
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c1.OnRoomCustomPropertiesChanged += changedProps => {
                    var props = c1.Room.CustomProperties;
                    Assert.AreEqual(props.GetString("name"), "leancloud");
                    Assert.AreEqual(props.GetInt("gold"), 1000);
                    f1 = true;
                };
                var newProps = new PlayObject {
                    { "name", "leancloud" },
                    { "gold", 1000 },
                };
                return c0.SetRoomCustomProperties(newProps);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f0 || !f1) {
                yield return null;
            }

            _ = c0.Close();
            _ = c1.Close();
        }

        [UnityTest]
        public IEnumerator ChangeRoomPropertiesWithCAS() {
            var f = false;
            var roomName = "cp1_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c = Utils.NewClient("cp1");

            c.Connect().OnSuccess(_ => {
                var options = new RoomOptions {
                    CustomRoomProperties = new PlayObject {
                        { "id", 1 },
                        { "gold", 100 }
                    }
                };
                return c.CreateRoom(roomName, options);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                var newProps = new PlayObject {
                    { "gold", 200 },
                };
                var expectedValues = new PlayObject {
                    { "id", 2 }
                };
                return c.SetRoomCustomProperties(newProps, expectedValues);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Assert.AreEqual(c.Room.CustomProperties["gold"], 100);
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
            _ = c.Close();
        }

        [UnityTest]
        public IEnumerator ChangePlayerProperties() {
            var f0 = false;
            var f1 = false;
            var roomName = "cp2_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c0 = Utils.NewClient("cp2_0");
            var c1 = Utils.NewClient("cp2_1");
            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c0.OnPlayerCustomPropertiesChanged += (player, changedProps) => {
                    var props = player.CustomProperties;
                    Assert.AreEqual(props.GetString("nickname"), "LeanCloud");
                    Assert.AreEqual(props.GetInt("gold"), 100);
                    var attr = props["attr"] as PlayObject;
                    Assert.AreEqual(attr.GetInt("hp"), 10);
                    Assert.AreEqual(attr.GetInt("mp"), 20);
                    Debug.Log("c0 check done");
                    f0 = true;
                };
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c1.OnPlayerCustomPropertiesChanged += (player, changedProps) => {
                    var p = player.CustomProperties;
                    Assert.AreEqual(p.GetString("nickname"), "LeanCloud");
                    Assert.AreEqual(p.GetInt("gold"), 100);
                    var attr = p["attr"] as PlayObject;
                    Assert.AreEqual(attr.GetInt("hp"), 10);
                    Assert.AreEqual(attr.GetInt("mp"), 20);
                    Debug.Log("c1 check done");
                    f1 = true;
                };
                var props = new PlayObject {
                    { "nickname", "LeanCloud" },
                    { "gold", 100 },
                    { "attr", new PlayObject {
                            { "hp", 10 },
                            { "mp", 20 }
                        } 
                    }
                };
                return c1.Player.SetCustomProperties(props);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f0 || !f1) {
                yield return null;
            }
            _ = c0.Close();
            _ = c1.Close();
        }

        [UnityTest]
        public IEnumerator ChangePlayerPropertiesWithCAS() {
            var f = false;
            var roomName = "cp3_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c = Utils.NewClient("cp3");
            c.Connect().OnSuccess(_ => {
                return c.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                var props = new PlayObject {
                    { "id", 1 },
                    { "nickname", "lean" }
                };
                return c.Player.SetCustomProperties(props);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                var newProps = new PlayObject {
                    { "nickname", "cloud" }
                };
                var expectedValues = new PlayObject {
                    { "id", 2 }
                };
                return c.Player.SetCustomProperties(newProps, expectedValues);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Assert.AreEqual(c.Player.CustomProperties["nickname"], "lean");
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
            _ = c.Close();
        }

        [UnityTest]
        public IEnumerator GetPlayerPropertiesWhenJoinRoom() {
            var f = false;
            var roomName = "cp4_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c0 = Utils.NewClient("cp4_0");
            var c1 = Utils.NewClient("cp4_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                var props = new PlayObject {
                    { "ready", true }
                };
                return c0.Player.SetCustomProperties(props);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                var master = c1.Room.Master;
                Assert.AreEqual(master.CustomProperties.GetBool("ready"), true);
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
            _ = c0.Close();
            _ = c1.Close();
        }

        [UnityTest]
        public IEnumerator ChangePropertiesWithSameValue() {
            var f = false;
            var roomName = "cp5_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c = Utils.NewClient("cp5");
            var props = new PlayObject {
                { "ready", true }
            };

            c.Connect().OnSuccess(_ => {
                return c.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c.Room.SetCustomProperties(props);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c.Room.SetCustomProperties(props);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => { 
                return c.Player.SetCustomProperties(props);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c.Player.SetCustomProperties(props);
                _ = c.Close();
                f = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SetNullProperty() {
            var f0 = false;
            var f1 = false;
            var roomName = "cp6_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c = Utils.NewClient("cp6");

            c.Connect().OnSuccess(_ => {
                return c.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c.OnRoomCustomPropertiesChanged += (__) => { 
                    if (c.Room.CustomProperties.GetString("name") == "leancloud") {
                        f0 = true;
                        Debug.Log("============== f0 is true");
                    }
                    if (c.Room.CustomProperties.IsNull("name")) {
                        f1 = true;
                        Debug.Log("============== f1 is true");
                    }
                };
                var props = new PlayObject {
                    { "name", "leancloud" }
                };
                return c.Room.SetCustomProperties(props);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                var props = new PlayObject {
                    { "name", null }
                };
                return c.Room.SetCustomProperties(props);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f0 || !f1) {
                yield return null;
            }

            _ = c.Close();
        }
    }
}