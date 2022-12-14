using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using LeanCloud.Play.Protocol;
using LC.Newtonsoft.Json;
using LC.Google.Protobuf;

namespace LeanCloud.Play {
    internal class GameConnection : BaseConnection {
        internal Room Room {
            get; private set;
        }

        internal GameConnection(string appId, string server, string gameVersion, string userId, string sessionToken)
            : base(appId, server, gameVersion, userId, sessionToken) {
        }

        internal async Task<Protocol.RoomOptions> CreateRoom(string roomId, RoomOptions roomOptions, List<string> expectedUserIds) {
            var request = NewRequest();
            var roomOpts = ConvertToRoomOptions(roomId, roomOptions, expectedUserIds);
            request.CreateRoom = new CreateRoomRequest {
                RoomOptions = roomOpts
            };
            var res = await SendRequest(CommandType.Conv, OpType.Start, request);
            return res.Response.CreateRoom.RoomOptions;
        }

        internal async Task<Protocol.RoomOptions> JoinRoom(string roomId, bool rejoin = false, List<string> expectedUserIds = null) {
            var request = NewRequest();
            request.JoinRoom = new JoinRoomRequest {
                Rejoin = rejoin,
                RoomOptions = new Protocol.RoomOptions {
                    Cid = roomId
                },
            };
            if (expectedUserIds != null) {
                request.JoinRoom.RoomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            var res = await SendRequest(CommandType.Conv, OpType.Add, request);
            return res.Response.JoinRoom.RoomOptions;
        }

        internal async Task LeaveRoom() {
            var request = NewRequest();
            await SendRequest(CommandType.Conv, OpType.Remove, request);
        }

        internal async Task<PlayObject> SetRoomOpen(bool open) {
            var request = NewRequest();
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty {
                    Open = open
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.Response.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> SetRoomVisible(bool visible) {
            var request = NewRequest();
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty {
                    Visible = visible
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.Response.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> SetRoomMaxPlayerCount(int count) {
            var request = NewRequest();
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty {
                    MaxMembers = count
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.Response.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> SetRoomExpectedUserIds(List<string> expectedUserIds) {
            var request = NewRequest();
            var args = new Dictionary<string, object> {
                { "$set", expectedUserIds.ToList<object>() }
            };
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty {
                    ExpectMembers = JsonConvert.SerializeObject(args)
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.Response.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> ClearRoomExpectedUserIds() {
            var request = NewRequest();
            var args = new Dictionary<string, object> {
                { "$drop", true }
            };
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty {
                    ExpectMembers = JsonConvert.SerializeObject(args)
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.Response.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> AddRoomExpectedUserIds(List<string> expectedUserIds) {
            var request = NewRequest();
            var args = new Dictionary<string, object> {
                { "$add", expectedUserIds.ToList<object>() }
            };
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty {
                    ExpectMembers = JsonConvert.SerializeObject(args)
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.Response.UpdateSysProperty.SysAttr);
        }

        internal async Task<PlayObject> RemoveRoomExpectedUserIds(List<string> expectedUserIds) {
            var request = NewRequest();
            var args = new Dictionary<string, object> {
                { "$remove", expectedUserIds.ToList<object>() }
            };
            request.UpdateSysProperty = new UpdateSysPropertyRequest {
                SysAttr = new RoomSystemProperty {
                    ExpectMembers = JsonConvert.SerializeObject(args)
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateSystemProperty, request);
            return Utils.ConvertToPlayObject(res.Response.UpdateSysProperty.SysAttr);
        }

        internal async Task<int> SetMaster(int newMasterId) {
            var request = NewRequest();
            request.UpdateMasterClient = new UpdateMasterClientRequest {
                MasterActorId = newMasterId
            };
            var res = await SendRequest(CommandType.Conv, OpType.UpdateMasterClient, request);
            return res.Response.UpdateMasterClient.MasterActorId;
        }

        internal async Task<int> KickPlayer(int actorId, int code, string reason) {
            var request = NewRequest();
            request.KickMember = new KickMemberRequest {
                TargetActorId = actorId,
                AppInfo = new AppInfo {
                    AppCode = code,
                    AppMsg = reason ?? string.Empty
                }
            };
            var res = await SendRequest(CommandType.Conv, OpType.Kick, request);
            return res.Response.KickMember.TargetActorId;
        }

        internal Task SendEvent(byte eventId, PlayObject eventData, SendEventOptions options) {
            var direct = new DirectCommand {
                EventId = eventId
            };
            if (eventData != null) {
                direct.Msg = ByteString.CopyFrom(CodecUtils.SerializePlayObject(eventData));
            }
            direct.ReceiverGroup = (int)options.ReceiverGroup;
            if (options.TargetActorIds != null) {
                direct.ToActorIds.AddRange(options.TargetActorIds);
            }
            _ = SendCommand(CommandType.Direct, OpType.None, new Body {
                Direct = direct
            });
            return Task.FromResult(true);
        }

        internal async Task<PlayObject> SetRoomCustomProperties(PlayObject properties, PlayObject expectedValues) {
            var request = NewRequest();
            request.UpdateProperty = new UpdatePropertyRequest {
                Attr = ByteString.CopyFrom(CodecUtils.SerializePlayObject(properties))
            };
            if (expectedValues != null) {
                request.UpdateProperty.ExpectAttr = ByteString.CopyFrom(CodecUtils.SerializePlayObject(expectedValues));
            }
            var res = await SendRequest(CommandType.Conv, OpType.Update, request);
            var props = CodecUtils.DeserializePlayObject(res.Response.UpdateProperty.Attr);
            return props;
        }

        internal async Task<Tuple<int, PlayObject>> SetPlayerCustomProperties(int playerId, PlayObject properties, PlayObject expectedValues) {
            var request = NewRequest();
            request.UpdateProperty = new UpdatePropertyRequest {
                TargetActorId = playerId,
                Attr = ByteString.CopyFrom(CodecUtils.SerializePlayObject(properties))
            };
            if (expectedValues != null) {
                request.UpdateProperty.ExpectAttr = ByteString.CopyFrom(CodecUtils.SerializePlayObject(expectedValues));
            }
            var res = await SendRequest(CommandType.Conv, OpType.UpdatePlayerProp, request);
            var actorId = res.Response.UpdateProperty.ActorId;
            var props = CodecUtils.DeserializePlayObject(res.Response.UpdateProperty.Attr);
            return new Tuple<int, PlayObject>(actorId, props);
        }

        protected override int KeepAliveInterval => 5 * 1000;

        protected override string GetFastOpenUrl(string server, string appId, string gameVersion, string userId, string sessionToken) {
            Uri uri = new Uri(server);
            string url = $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}";
            NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
            Dictionary<string, string> query = nameValueCollection.AllKeys.ToDictionary(k => k, k => nameValueCollection[k]);
            Dictionary<string, string> parameters = new Dictionary<string, string> {
                { "appId", appId },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion },
                { "gameVersion", gameVersion },
                { "userId", userId },
                { "sessionToken", sessionToken }
            };
            Dictionary<string, string> queries = parameters.Concat(query.Where(entry => !parameters.ContainsKey(entry.Key)))
                .ToDictionary(entry => entry.Key, entry => entry.Value);
            return $"{url}session?{string.Join("&", queries.Select(entry => $"{entry.Key}={entry.Value}"))}";
        }

        protected override void HandleNotification(CommandType cmd, OpType op, Body body) {
            OnNotification?.Invoke(cmd, op, body);
        }

        static Protocol.RoomOptions ConvertToRoomOptions(string roomName, RoomOptions options, List<string> expectedUserIds) {
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
                if (options.CustomRoomProperties != null) {
                    roomOptions.Attr = ByteString.CopyFrom(CodecUtils.SerializePlayObject(options.CustomRoomProperties));
                }
                if (options.CustomRoomPropertyKeysForLobby != null) {
                    roomOptions.LobbyAttrKeys.AddRange(options.CustomRoomPropertyKeysForLobby);
                }
                if (options.PluginName != null) {
                    roomOptions.PluginName = options.PluginName;
                }
            }
            if (expectedUserIds != null) {
                roomOptions.ExpectMembers.AddRange(expectedUserIds);
            }
            return roomOptions;
        }
    }
}
