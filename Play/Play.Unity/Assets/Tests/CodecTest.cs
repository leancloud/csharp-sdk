using NUnit.Framework;
using UnityEngine;
using LC.Google.Protobuf;
using LeanCloud.Play.Protocol;
using System.Collections.Generic;
using System.Collections;
using System;

namespace LeanCloud.Play {
    public class CodecTest {
        class Hero {
            public string Name {
                get; set;
            }

            public float Score {
                get; set;
            }

            public int Hp {
                get; set;
            }

            public int Mp {
                get; set;
            }

            public List<Weapon> Weapons {
                get; set;
            }

            public static byte[] Serialize(object obj) {
                Hero hero = obj as Hero;
                var playObject = new PlayObject {
                    { "name", hero.Name },
                    { "score", hero.Score },
                    { "hp", hero.Hp },
                    { "mp", hero.Mp },
                    { "weapons", new PlayArray(hero.Weapons) }
                };
                return CodecUtils.SerializePlayObject(playObject);
            }

            public static object Deserialize(byte[] bytes) {
                var playObject = CodecUtils.DeserializePlayObject(bytes);
                Hero hero = new Hero {
                    Name = playObject.GetString("name"),
                    Score = playObject.GetFloat("score"),
                    Hp = playObject.GetInt("hp"),
                    Mp = playObject.GetInt("mp"),
                    Weapons = playObject.GetPlayArray("weapons").ToList<Weapon>()
                };
                return hero;
            }
        }

        class Weapon {
            public string Name {
                get; set;
            }

            public int Attack {
                get; set;
            }

            public static byte[] Serialize(object obj) {
                Weapon weapon = obj as Weapon;
                var playObject = new PlayObject {
                    { "name", weapon.Name },
                    { "attack", weapon.Attack }
                };
                return CodecUtils.SerializePlayObject(playObject);
            }

            public static object Deserialize(byte[] bytes) {
                var playObject = CodecUtils.DeserializePlayObject(bytes);
                Weapon weapon = new Weapon {
                    Name = playObject.GetString("name"),
                    Attack = playObject.GetInt("attack")
                };
                return weapon;
            }
        }

        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Log;
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Log;
        }

        [Test]
        public void CheckType() {
            object s = (short)10;
            object a = 10;
            object l = 10L;
            object f = 10f;
            Assert.AreEqual(s is short, true);
            Assert.AreEqual(a is int, true);
            Assert.AreEqual(l is long, true);
            Assert.AreEqual(f is float, true);
            Assert.AreEqual(f is double, false);
            var sl = new List<string> {
                "aaa", "bbb", "ccc"
            };
            Assert.AreEqual(sl is IList, true);
            var isl = (IList)sl;
            foreach (var v in isl) {
                Debug.Log(v);
            }
            var sd = new Dictionary<string, string> {
                { "aa", "bb" },
                { "cc", "dd" }
            };
            Assert.AreEqual(sd is IDictionary, true);
            var isd = (IDictionary)sd;
            foreach (var key in isd.Keys) {
                Debug.Log($"{key} : {isd[key]}");
            }
        }

        [Test]
        public void PlayObject() {
            var playObj = new PlayObject {
                ["i"] = 123,
                ["b"] = true,
                ["str"] = "hello, world"
            };
            var subPlayObj = new PlayObject {
                ["si"] = 345,
                ["sb"] = true,
                ["sstr"] = "code"
            };
            playObj.Add("obj", subPlayObj);
            var subPlayArr = new PlayArray {
                666, true, "engineer"
            };
            playObj.Add("arr", subPlayArr);
            var genericValue = CodecUtils.Serialize(playObj);
            Debug.Log(genericValue);
            var newPlayObj = CodecUtils.Deserialize(genericValue) as PlayObject;
            Assert.AreEqual(newPlayObj["i"], 123);
            Assert.AreEqual(newPlayObj["b"], true);
            Assert.AreEqual(newPlayObj["str"], "hello, world");
            var newSubPlayObj = newPlayObj["obj"] as PlayObject;
            Assert.AreEqual(newSubPlayObj["si"], 345);
            Assert.AreEqual(newSubPlayObj["sb"], true);
            Assert.AreEqual(newSubPlayObj["sstr"], "code");
            var newSubPlayArr = newPlayObj["arr"] as PlayArray;
            Assert.AreEqual(newSubPlayArr[0], 666);
            Assert.AreEqual(newSubPlayArr[1], true);
            Assert.AreEqual(newSubPlayArr[2], "engineer");
            // Dictionary to PlayObject
            var dict = new Dictionary<string, int> {
                { "hello", 123 },
                { "world", 456 }
            };
            var dictObj = new PlayObject(dict);
            Assert.AreEqual(dictObj["hello"], 123);
            Assert.AreEqual(dictObj["world"], 456);
        }

        [Test]
        public void PlayArray() {
            var playArr = new PlayArray {
                123, true, "hello, world",
                new PlayObject {
                    ["i"] = 23,
                    ["b"] = true,
                    ["str"] = "hello"
                }
            };
            var genericValue = CodecUtils.Serialize(playArr);
            Debug.Log(genericValue);
            var newPlayArr = CodecUtils.Deserialize(genericValue) as PlayArray;
            Assert.AreEqual(newPlayArr[0], 123);
            Assert.AreEqual(newPlayArr[1], true);
            Assert.AreEqual(newPlayArr[2], "hello, world");
            var subPlayObj = newPlayArr[3] as PlayObject;
            Assert.AreEqual(subPlayObj["i"], 23);
            Assert.AreEqual(subPlayObj["b"], true);
            Assert.AreEqual(subPlayObj["str"], "hello");
            // List to PlayArray
            var iList = new List<int> { 10, 24 };
            var iArr = new PlayArray(iList);
            Assert.AreEqual(iArr[0], 10);
            Assert.AreEqual(iArr[1], 24);
            var sList = new List<string> { "hello", "world" };
            var sArr = new PlayArray(sList);
            Assert.AreEqual(sArr[0], "hello");
            Assert.AreEqual(sArr[1], "world");
        }

        [Test]
        public void Protocol() {
            // 构造请求
            var request = new RequestMessage() {
                I = 1,
            };
            var roomOptions = new RoomOptions {
                Visible = false,
                EmptyRoomTtl = 60,
                MaxPlayerCount = 2,
                PlayerTtl = 60,
                CustomRoomProperties = new PlayObject {
                    { "title", "room title" },
                    { "level", 2 },
                },
                CustomRoomPropertyKeysForLobby = new List<string> { "level" }
            };
            var expectedUserIds = new List<string> { "world" };
            var roomOpts = ConvertToRoomOptions("abc", roomOptions, expectedUserIds);
            request.CreateRoom = new CreateRoomRequest {
                RoomOptions = roomOpts
            };
            var command = new Command {
                Cmd = CommandType.Conv,
                Op = OpType.Start,
                Body = new Body {
                    Request = request
                }.ToByteString()
            };
            // 序列化请求
            var bytes = command.ToByteArray();
            // 反序列化请求
            var reCommand = Command.Parser.ParseFrom(bytes);
            Assert.AreEqual(reCommand.Cmd, CommandType.Conv);
            Assert.AreEqual(reCommand.Op, OpType.Start);
            var reBody = Body.Parser.ParseFrom(reCommand.Body);
            var reRequest = reBody.Request;
            Assert.AreEqual(reRequest.I, 1);
            var reRoomOptions = request.CreateRoom.RoomOptions;
            Assert.AreEqual(reRoomOptions.Visible, false);
            Assert.AreEqual(reRoomOptions.EmptyRoomTtl, 60);
            Assert.AreEqual(reRoomOptions.MaxMembers, 2);
            Assert.AreEqual(reRoomOptions.PlayerTtl, 60);
            var attrBytes = reRoomOptions.Attr;
            var reAttr = CodecUtils.Deserialize(GenericCollectionValue.Parser.ParseFrom(attrBytes)) as PlayObject;
            Debug.Log(reAttr["title"]);
            Debug.Log(reAttr["level"]);
            Assert.AreEqual(reAttr["title"], "room title");
            Assert.AreEqual(reAttr["level"], 2);
        }

        [Test]
        public void CustomType() {
            CodecUtils.RegisterType(typeof(Hero), 10, Hero.Serialize, Hero.Deserialize);
            CodecUtils.RegisterType(typeof(Weapon), 11, Weapon.Serialize, Weapon.Deserialize);
            var hero = new Hero {
                Name = "Li Lei",
                Score = 99.9f,
                Hp = 10,
                Mp = 8,
                Weapons = new List<Weapon> {
                    new Weapon {
                        Name = "pen",
                        Attack = 100
                    },
                    new Weapon {
                        Name = "erase",
                        Attack = 200
                    }
                }
            };
            var data = CodecUtils.Serialize(hero);
            var newHero = CodecUtils.Deserialize(data) as Hero;
            Assert.AreEqual(newHero.Name, "Li Lei");
            Assert.AreEqual(Math.Abs(newHero.Score - 99.9f) < float.Epsilon, true);
            Assert.AreEqual(newHero.Hp, 10);
            Assert.AreEqual(newHero.Mp, 8);
            var pen = newHero.Weapons[0];
            Assert.AreEqual(pen.Name, "pen");
            Assert.AreEqual(pen.Attack, 100);
            var erase = newHero.Weapons[1];
            Assert.AreEqual(erase.Name, "erase");
            Assert.AreEqual(erase.Attack, 200);
        }

        internal static Protocol.RoomOptions ConvertToRoomOptions(string roomName, RoomOptions options, List<string> expectedUserIds) {
            var roomOptions = new Protocol.RoomOptions();
            if (!string.IsNullOrEmpty(roomName)) {
                roomOptions.Cid = roomName;
            }
            if (options != null) {
                roomOptions.Visible = options.Visible;
                roomOptions.Open = options.Open;
                roomOptions.EmptyRoomTtl = options.EmptyRoomTtl;
                roomOptions.PlayerTtl = options.PlayerTtl;
                roomOptions.MaxMembers = options.MaxPlayerCount;
                roomOptions.Flag = options.Flag;
                roomOptions.Attr = CodecUtils.Serialize(options.CustomRoomProperties).ToByteString();
                roomOptions.LobbyAttrKeys.AddRange(options.CustomRoomPropertyKeysForLobby);
            }
            if (expectedUserIds != null) {
                roomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            return roomOptions;
        }
    }
}
