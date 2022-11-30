using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;

namespace LeanCloud.Play {
    public class MasterTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator SetNewMaster() {
            var f0 = false;
            var f1 = false;
            var roomName = "mt0_r";
            var c0 = Utils.NewClient("mt0_0");
            var c1 = Utils.NewClient("mt0_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(t => {
                var room = t.Result;
                c0.OnMasterSwitched += newMaster => {
                    Assert.AreEqual(newMaster.ActorId, c1.Player.ActorId);
                    Assert.AreEqual(newMaster.ActorId, c0.Room.MasterActorId);
                    f0 = true;
                };
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c1.OnMasterSwitched += newMaster => {
                    Assert.AreEqual(newMaster.ActorId, c1.Player.ActorId);
                    Assert.AreEqual(newMaster.ActorId, c1.Room.MasterActorId);
                    f1 = true;
                };
                return c0.SetMaster(c1.Player.ActorId);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log("set master done");
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f0 || !f1) {
                yield return null;
            }
            _ = c0.Close();
            _ = c1.Close();
        }

        [UnityTest]
        public IEnumerator MasterLeave() {
            var f0 = false;
            var f1 = false;
            var roomName = "mt1_r";
            var c0 = Utils.NewClient("mt1_0");
            var c1 = Utils.NewClient("mt1_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log("---------------------------");
                Debug.Log(c1.Room.MasterActorId);
                Debug.Log(c0.Player.ActorId);
                Debug.Log($"c1 joined, {c1.Room.MasterActorId}, {c0.Player.ActorId}");
                Assert.AreEqual(c1.Room.MasterActorId, c0.Player.ActorId);
                c1.OnMasterSwitched += newMaster => {
                    Assert.AreEqual(newMaster.ActorId, c1.Player.ActorId);
                    f1 = true;
                };
                return c0.LeaveRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log("leave room done");
                f0 = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f0 || !f1) {
                yield return null;
            }
            _ = c0.Close();
            _ = c1.Close();
        }

        [UnityTest]
        public IEnumerator FixMaster() {
            var f0 = false;
            var f1 = false;
            var roomName = "mt2_r";
            var c0 = Utils.NewClient("mt2_0");
            var c1 = Utils.NewClient("mt2_1");

            c0.Connect().OnSuccess(_ => {
                var options = new RoomOptions {
                    Flag = CreateRoomFlag.FixedMaster
                };
                return c0.CreateRoom(roomName, options);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c1.OnPlayerRoomLeft += leftPlayer => {
                    Assert.AreEqual(leftPlayer.ActorId, c0.Player.ActorId);
                };
                c1.OnMasterSwitched += newMaster => {
                    Assert.AreEqual(c1.Room.MasterActorId, 0);
                    Assert.AreEqual(newMaster, null);
                    f1 = true;
                };
                return c0.LeaveRoom();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log("leave room done");
                f0 = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f0 || !f1) {
                yield return null;
            }
            _ = c0.Close();
            _ = c1.Close();
        }
    }
}
