using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;

namespace LeanCloud.Play {
    public class KickTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator Kick() {
            var f1 = false;
            var f2 = false;
            var roomName = "kt0_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c0 = Utils.NewClient("kt0_0");
            var c1 = Utils.NewClient("kt0_1");
            var c2 = Utils.NewClient("kt0_2");
            _ = c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c1.OnRoomKicked += (code, msg) => {
                    Debug.Log($"{c1.UserId} is kicked");
                    Assert.AreEqual(code, null);
                    Assert.AreEqual(msg, null);
                    _ = c0.Close();
                    _ = c1.Close();
                    f1 = true;
                };
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log($"{c1.UserId} joined room");
                return c2.Connect();

            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c2.OnPlayerRoomLeft += leftPlayer => {
                    Debug.Log($"{leftPlayer.UserId} is left");
                    f2 = true;
                };
                return c2.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log($"{c2.UserId} joined room");
                _ = c0.KickPlayer(c1.Player.ActorId);
            });

            while (!f1 || !f2) {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator KickWithMsg() {
            var flag = false;
            var roomName = "kt1_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c0 = Utils.NewClient("kt1_0");
            var c1 = Utils.NewClient("kt1_1");
            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c0.OnPlayerRoomJoined += newPlayer => {
                    _ = c0.KickPlayer(newPlayer.ActorId, 404, "You cheat!");
                };
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c1.OnRoomKicked += (code, msg) => {
                    Assert.AreEqual(code, 404);
                    Debug.Log($"{c1.UserId} is kicked for {msg}");
                    _ = c0.Close();
                    _ = c1.Close();
                    flag = true;
                };
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log($"{c1.UserId} joined room");
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!flag) {
                yield return null;
            }
        }
    }
}
