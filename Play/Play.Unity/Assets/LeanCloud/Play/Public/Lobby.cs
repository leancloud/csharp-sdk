using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Play.Protocol;

namespace LeanCloud.Play {
	internal class Lobby {
        enum State {
            Init,
            Joining,
            Lobby,
            Leaving,
            Closed
        }

        Client Client {
			get;
        }

        State state;
        LobbyConnection lobbyConn;

        internal List<LobbyRoom> LobbyRoomList {
            get; private set;
        }

		internal Lobby(Client client) {
			Client = client;
            state = State.Init;
        }

        internal async Task Join() {
            if (state == State.Joining || state == State.Lobby) {
                return;
            } 
            state = State.Joining;
            LobbyInfo lobbyInfo;
            try {
                lobbyInfo = await Client.lobbyService.Authorize();
            } catch (Exception e) {
                state = State.Init;
                throw e;
            }
            try {
                lobbyConn = new LobbyConnection(Client.AppId, lobbyInfo.Url, Client.GameVersion, Client.UserId, lobbyInfo.SessionToken);
                await lobbyConn.Connect();
                await lobbyConn.JoinLobby();
                lobbyConn.OnNotification = (cmd, op, body) => {
                    switch (cmd) {
                        case CommandType.Lobby:
                            switch (op) {
                                case OpType.RoomList:
                                    HandleRoomListUpdated(body.RoomList);
                                    break;
                                default:
                                    LCLogger.Error("unknown msg: {0}/{1} {2}", cmd, op, body);
                                    break;
                            }
                            break;
                        case CommandType.Statistic:
                            break;
                        case CommandType.Error: {
                                LCLogger.Error("error msg: {0}", body);
                                ErrorInfo errorInfo = body.Error.ErrorInfo;
                                Client.OnError?.Invoke(errorInfo.ReasonCode, errorInfo.Detail);
                            }
                            break;
                        default:
                            LCLogger.Error("unknown msg: {0}/{1} {2}", cmd, op, body);
                            break;
                    }
                };
                state = State.Lobby;
            } catch (Exception e) {
                if (lobbyConn != null) {
                    await lobbyConn.Close();
                }
                state = State.Init;
                throw e;
            }
        }

        internal async Task Leave() {
            try {
                await lobbyConn.LeaveLobby();
            } finally {
                await Close();
            }
        }

        internal async Task Close() {
            try {
                await lobbyConn.Close();
            } catch (Exception e) {
                LCLogger.Error(e.Message);
            }
        }

        void HandleRoomListUpdated(RoomListCommand notification) {
            List<LobbyRoom> list = new List<LobbyRoom>();
            foreach (var roomOpts in notification.List) {
                var lobbyRoom = ConvertToLobbyRoom(roomOpts);
                list.Add(lobbyRoom);
            }
            Client.OnLobbyRoomListUpdated?.Invoke(list);
        }

        LobbyRoom ConvertToLobbyRoom(Protocol.RoomOptions options) {
            var lobbyRoom = new LobbyRoom {
                RoomName = options.Cid,
                Open = options.Open == null || options.Open.Value,
                Visible = options.Visible == null || options.Visible.Value,
                MaxPlayerCount = options.MaxMembers,
                PlayerCount = options.MemberCount,
                EmptyRoomTtl = options.EmptyRoomTtl,
                PlayerTtl = options.PlayerTtl
            };
            if (options.ExpectMembers != null) {
                lobbyRoom.ExpectedUserIds = options.ExpectMembers.ToList<string>();
            }
            if (options.Attr != null) {
                lobbyRoom.CustomRoomProperties = CodecUtils.DeserializePlayObject(options.Attr);
            }
            return lobbyRoom;
        }
    }
}
