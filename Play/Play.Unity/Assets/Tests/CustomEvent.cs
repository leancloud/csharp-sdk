using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;

namespace LeanCloud.Play {
    public class CustomEvent {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Log;
        }

        [UnityTest]
        public IEnumerator CustomEventWithReceiverGroup() {
            var f = false;
            var roomName = "ce0_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c0 = Utils.NewClient("ce0_0");
            var c1 = Utils.NewClient("ce0_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c0.OnCustomEvent += (eventId, eventData, senderId) => {
                    Assert.AreEqual(eventId, 1);
                    Assert.AreEqual(eventData["name"], "aaa");
                    Assert.AreEqual(eventData["count"], 100);
                    f = true;
                };
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                var eventData = new PlayObject {
                    { "name", "aaa" },
                    { "count", 100 },
                };
                var options = new SendEventOptions { 
                    ReceiverGroup = ReceiverGroup.MasterClient
                };
                return c1.SendEvent(1, eventData, options);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log("send event done");
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
            _ = c0.Close();
            _ = c1.Close();
        }

        [UnityTest]
        public IEnumerator CustomEventWithTargetIds() {
            var f = false;
            var roomName = "ce1_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c0 = Utils.NewClient("ce1_0");
            var c1 = Utils.NewClient("ce1_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c0.OnCustomEvent += (eventId, eventData, senderId) => {
                    Assert.AreEqual(eventId, 2);
                    Assert.AreEqual(eventData["name"], "aaa");
                    Assert.AreEqual(eventData["count"], 100);
                    f = true;
                };
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                var eventData = new PlayObject {
                    { "name", "aaa" },
                    { "count", 100 },
                };
                var options = new SendEventOptions {
                    TargetActorIds = new List<int> { 1, 2 }
                };
                return c1.SendEvent(2, eventData, options);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log("send event done");
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f) {
                yield return null;
            }
            _ = c0.Close();
            _ = c1.Close();
        }


        [UnityTest]
        public IEnumerator SimpleEvent() {
            var f0 = false;
            var f1 = false;
            var roomName = "ce2_r" + Guid.NewGuid().ToString().Substring(0, 8);
            var c0 = Utils.NewClient("ce2_0");
            var c1 = Utils.NewClient("ce2_1");

            c0.Connect().OnSuccess(_ => {
                return c0.CreateRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c0.OnCustomEvent += (eventId, eventData, senderId) => {
                    Assert.AreEqual(eventId, 3);
                    f0 = true;
                };
                return c1.Connect();
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                return c1.JoinRoom(roomName);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                c1.OnCustomEvent += (eventId, eventData, senderId) => {
                    Assert.AreEqual(eventId, 3);
                    f1 = true;
                };
                return c1.SendEvent(3);
            }, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap().OnSuccess(_ => {
                Debug.Log("send event done");
            }, TaskScheduler.FromCurrentSynchronizationContext());

            while (!f0 || !f1) {
                yield return null;
            }
            _ = c0.Close();
            _ = c1.Close();
        }
    }
}
