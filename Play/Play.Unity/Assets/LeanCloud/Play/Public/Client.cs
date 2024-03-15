using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LeanCloud.Play {
    /// <summary>
    /// 客户端类
    /// </summary>
    public class Client {
        // 事件
        /// <summary>
        /// 大厅房间列表更新事件
        /// </summary>
        public Action<List<LobbyRoom>> OnLobbyRoomListUpdated;
        /// <summary>
        /// 有玩家加入房间事件
        /// </summary>
        public Action<Player> OnPlayerRoomJoined;
        /// <summary>
        /// 有玩家离开房间事件
        /// </summary>
        public Action<Player> OnPlayerRoomLeft;
        /// <summary>
        /// 房主切换事件
        /// </summary>
        public Action<Player> OnMasterSwitched;
        /// <summary>
        /// 房间自定义属性更新事件
        /// </summary>
        public Action<PlayObject> OnRoomCustomPropertiesChanged;
        /// <summary>
        /// 房间系统属性更新事件，目前包括：房间开关，可见性，最大玩家数量，预留玩家 Id 列表
        /// </summary>
        public Action<PlayObject> OnRoomSystemPropertiesChanged;
        /// <summary>
        /// 玩家自定义属性更新事件
        /// </summary>
        public Action<Player, PlayObject> OnPlayerCustomPropertiesChanged;
        /// <summary>
        /// 玩家在线 / 离线变化事件
        /// </summary>
        public Action<Player> OnPlayerActivityChanged;
        /// <summary>
        /// 用户自定义事件
        /// </summary>
        public Action<byte, PlayObject, int> OnCustomEvent;
        /// <summary>
        /// 被踢出房间事件
        /// </summary>
        public Action<int?, string> OnRoomKicked;
        /// <summary>
        /// 断线事件
        /// </summary>
        public Action OnDisconnected;
        /// <summary>
        /// 错误事件
        /// </summary>
        public Action<int, string> OnError;

        internal LobbyService lobbyService;

        public string PlayServer {
            get; private set;
        }

        /// <summary>
        /// LeanCloud App Id
        /// </summary>
        /// <value>App Id</value>
        public string AppId {
            get; private set;
        }

        /// <summary>
        /// LeanCloud App Key
        /// </summary>
        /// <value>App Key</value>
        public string AppKey {
            get; private set;
        }

        /// <summary>
        /// 用户唯一 Id
        /// </summary>
        /// <value>玩家唯一 Id</value>
        public string UserId {
            get; private set;
        }

        /// <summary>
        /// 是否启用 SSL
        /// </summary>
        /// <value>如果开启 SSL，则设为 true；否则设为 false。默认是 true</value>
        public bool Ssl {
            get; private set;
        }

        /// <summary>
        /// 客户端版本号，不同的版本号的玩家不会匹配到相同的房间
        /// </summary>
        /// <value>游戏版本号</value>
        public string GameVersion {
            get; private set;
        }   

        /// <summary>
        /// 大厅房间列表
        /// </summary>
        /// <value>可加入的房间列表</value>
        public List<LobbyRoom> LobbyRoomList {
            get {
                if (lobby == null) {
                    return null;
                }
                return lobby.LobbyRoomList;
            }
        }

        Lobby lobby;

        /// <summary>
        /// 当前房间对象
        /// </summary>
        /// <value>当前房间</value>
        public Room Room {
            get; internal set;
        }

        /// <summary>
        /// 当前玩家对象
        /// </summary>
        /// <value>当前玩家</value>
        public Player Player {
            get {
                return Room.Player;
            }
        }

        /// <summary>
        /// Client 构造方法
        /// </summary>
        /// <param name="appId">LeanCloud App Id</param>
        /// <param name="appKey">LeanCloud App Key</param>
        /// <param name="userId">用户唯一 Id</param>
        /// <param name="ssl">是否启用 SSL</param>
        /// <param name="gameVersion">游戏版本号</param>
        /// <param name="playServer">游戏服务器地址</param>
        public Client(string appId, string appKey, string userId, bool ssl = true, string gameVersion = "0.0.1", string playServer = null) {
            AppId = appId;
            AppKey = appKey;
            UserId = userId;
            Ssl = ssl;
            GameVersion = gameVersion;
            PlayServer = playServer;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns>客户端</returns>
        public async Task<Client> Connect() {
            lobbyService = new LobbyService(this);
            await lobbyService.Authorize();
            return this;
        }

        /// <summary>
        /// 加入大厅，会接收到大厅房间列表更新的事件
        /// </summary>
        public async Task JoinLobby() {
            try {
                if (lobby != null) {
                    throw new Exception("You are already in lobby.");
                }
                lobby = new Lobby(this);
                await lobby.Join();
            } catch (Exception e) {
                LCLogger.Error(e.Message);
            }
        }

        /// <summary>
        /// 离开大厅
        /// </summary>
        public async Task LeaveLobby() {
            if (lobby == null) {
                throw new Exception("You are not in lobby yet.");
            }
            await lobby.Leave();
            lobby = null;
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="roomName">房间唯一 Id</param>
        /// <param name="roomOptions">创建房间选项</param>
        /// <param name="expectedUserIds">期望用户 Id 列表</param>
        /// <returns>房间</returns>
        public async Task<Room> CreateRoom(string roomName = null, RoomOptions roomOptions = null, List<string> expectedUserIds = null) {
            if (Room != null) {
                throw new Exception("You are already in room.");
            }
            // 关闭 Lobby
            if (lobby != null) {
                await lobby.Close();
            }
            try {
                Room = new Room(this);
                await Room.Create(roomName, roomOptions, expectedUserIds);
                return Room;
            } catch (Exception e) {
                Room = null;
                throw e;
            }
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        /// <param name="roomName">房间 Id</param>
        /// <param name="expectedUserIds">期望用户 Id 列表</param>
        /// <returns>房间</returns>
        public async Task<Room> JoinRoom(string roomName, List<string> expectedUserIds = null) {
            if (Room != null) {
                throw new Exception("You are already in room.");
            }
            // 关闭 Lobby
            if (lobby != null) {
                await lobby.Close();
            }
            try {
                Room = new Room(this);
                await Room.Join(roomName, expectedUserIds);
                return Room;
            } catch (Exception e) {
                Room = null;
                throw e;
            }
        }

        /// <summary>
        /// 返回房间
        /// </summary>
        /// <param name="roomName">房间 Id</param>
        /// <returns>房间</returns>
        public async Task<Room> RejoinRoom(string roomName) {
            // 关闭 Lobby
            if (lobby != null) {
                await lobby.Close();
            }
            try {
                Room = new Room(this);
                await Room.Rejoin(roomName);
                return Room;
            } catch (Exception e) {
                Room = null;
                throw e;
            }
        }

        /// <summary>
        /// 加入或创建房间，如果房间 Id 存在，则加入；否则根据 roomOptions 和 expectedUserIds 创建新的房间
        /// </summary>
        /// <param name="roomName">房间 Id</param>
        /// <param name="roomOptions">创建房间选项</param>
        /// <param name="expectedUserIds">期望用户 Id 列表</param>
        /// <returns>房间</returns>
        public async Task<Room> JoinOrCreateRoom(string roomName, RoomOptions roomOptions = null, List<string> expectedUserIds = null) {
            if (Room != null) {
                throw new Exception("You are already in room.");
            }
            // 关闭 Lobby
            if (lobby != null) {
                await lobby.Close();
            }
            try {
                Room = new Room(this);
                await Room.JoinOrCreate(roomName, roomOptions, expectedUserIds);
                return Room;
            } catch (Exception e) {
                Room = null;
                throw e;
            }
        }

        /// <summary>
        /// 随机加入房间
        /// </summary>
        /// <param name="matchProperties">匹配属性</param>
        /// <param name="expectedUserIds">期望用户 Id 列表</param>
        /// <returns>房间</returns>
        public async Task<Room> JoinRandomRoom(PlayObject matchProperties = null, List<string> expectedUserIds = null) {
            if (Room != null) {
                throw new Exception("You are already in room.");
            }
            // 关闭 Lobby
            if (lobby != null) {
                await lobby.Close();
            }
            try {
                Room = new Room(this);
                await Room.JoinRandom(matchProperties, expectedUserIds);
                return Room;
            } catch (Exception e) {
                Room = null;
                throw e;
            }
        }

        /// <summary>
        /// 重连并返回上一个加入的房间
        /// </summary>
        /// <returns>房间</returns>
        public async Task<Room> ReconnectAndRejoin() {
            if (Room == null) {
                throw new ArgumentNullException(nameof(Room));
            }
            await Connect();
            var room = await RejoinRoom(Room.Name);
            return room;
        }

        /// <summary>
        /// 匹配房间（不加入）
        /// </summary>
        /// <param name="piggybackUserId">占位用户 Id</param>
        /// <param name="matchProperties">匹配属性</param>
        /// <returns>房间 Id</returns>
        public async Task<string> MatchRandom(string piggybackUserId, PlayObject matchProperties = null, List<string> expectedUserIds = null) {
            var lobbyRoom = await lobbyService.MatchRandom(piggybackUserId, matchProperties, expectedUserIds);
            return lobbyRoom.RoomId;
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        public async Task LeaveRoom() {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            await Room.Leave();
        }

        /// <summary>
        /// 获取玩家当前房间
        /// </summary>
        /// <returns>房间 id</returns>
        public async Task<string> FetchMyRoom() {
            LobbyRoomResult room = await lobbyService.FetchMyRoom();
            return room.RoomId;
        }

        /// <summary>
        /// 设置房间开启 / 关闭
        /// </summary>
        /// <returns>房间是否开启</returns>
        /// <param name="open">是否开启</param>
        public async Task<bool> SetRoomOpen(bool open) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            return await Room.SetOpen(open);
        } 

        /// <summary>
        /// 设置房间可见性
        /// </summary>
        /// <returns>房间是否可见</returns>
        /// <param name="visible">是否可见</param>
        public async Task<bool> SetRoomVisible(bool visible) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            return await Room.SetVisible(visible);
        }

        /// <summary>
        /// 设置房间最大玩家数量
        /// </summary>
        /// <returns>房间可容纳的最大人数</returns>
        /// <param name="count">数量</param>
        public async Task<int> SetRoomMaxPlayerCount(int count) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            return await Room.SetMaxPlayerCount(count);
        }

        /// <summary>
        /// 设置期望用户
        /// </summary>
        /// <returns>房间当前期望加入玩家列表</returns>
        /// <param name="expectedUserIds">期望用户 Id 列表</param>
        public async Task<List<string>> SetRoomExpectedUserIds(List<string> expectedUserIds) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            return await Room.SetExpectedUserIds(expectedUserIds);
        }

        /// <summary>
        /// 清空期望用户 Id 列表
        /// </summary>
        public async Task ClearRoomExpectedUserIds() {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            await Room.ClearExpectedUserIds();
        }

        /// <summary>
        /// 增加期望用户
        /// </summary>
        /// <returns>房间当前期望加入玩家列表</returns>
        /// <param name="expectedUserIds">增加的期望用户 Id 列表</param>
        public async Task<List<string>> AddRoomExpectedUserIds(List<string> expectedUserIds) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            return await Room.AddExpectedUserIds(expectedUserIds);
        }

        /// <summary>
        /// 删除期望用户
        /// </summary>
        /// <returns>房间当前期望加入玩家列表</returns>
        /// <param name="expectedUserIds">删除的期望用户 Id 列表</param>
        public async Task<List<string>> RemoveRoomExpectedUserIds(List<string> expectedUserIds) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            return await Room.RemoveExpectedUserIds(expectedUserIds);
        }

        /// <summary>
        /// 设置房主
        /// </summary>
        /// <returns>房主</returns>
        /// <param name="newMasterId">新房主的 Actor Id</param>
        public async Task<Player> SetMaster(int newMasterId) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            return await Room.SetMaster(newMasterId);
        }

        /// <summary>
        /// 将玩家踢出房间
        /// </summary>
        /// <param name="actorId">玩家的 Actor Id</param>
        /// <param name="code">附加码</param>
        /// <param name="reason">附加消息</param>
        public async Task KickPlayer(int actorId, int code = 0, string reason = null) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            await Room.KickPlayer(actorId, code, reason);
        }

        /// <summary>
        /// 发送自定义事件
        /// </summary>
        /// <param name="eventId">事件 Id</param>
        /// <param name="eventData">事件参数</param>
        /// <param name="options">事件选项</param>
        public Task SendEvent(byte eventId, PlayObject eventData = null, SendEventOptions options = null) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            return Room.SendEvent(eventId, eventData, options);
        }

        /// <summary>
        /// 设置房间自定义属性
        /// </summary>
        /// <param name="properties">自定义属性</param>
        /// <param name="expectedValues">用于 CAS 的期望属性</param>
        public async Task SetRoomCustomProperties(PlayObject properties, PlayObject expectedValues = null) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            await Room.SetCustomProperties(properties, expectedValues);
        }

        /// <summary>
        /// 设置玩家自定义属性
        /// </summary>
        /// <param name="actorId">玩家 Actor Id</param>
        /// <param name="properties">自定义属性</param>
        /// <param name="expectedValues">用于 CAS 的期望属性</param>
        public async Task SetPlayerCustomProperties(int actorId, PlayObject properties, PlayObject expectedValues = null) {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            await Room.SetPlayerCustomProperties(actorId, properties, expectedValues);
        }

        /// <summary>
        /// 暂停消息队列
        /// </summary>
        public void PauseMessageQueue() {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            Room.PauseMessageQueue();
        }

        /// <summary>
        /// 恢复消息队列
        /// </summary>
        public void ResumeMessageQueue() {
            if (Room == null) {
                throw new Exception("You are not in room yet.");
            }
            Room.ResumeMessageQueue();
        }

        /// <summary>
        /// 关闭服务
        /// </summary>
        public async Task Close() {
            // Clear
            AppId = null;
            AppKey = null;
            UserId = null;
            GameVersion = null;
            lobbyService = null;

            // 事件解注册
            OnLobbyRoomListUpdated = null;
            OnPlayerRoomJoined = null;
            OnPlayerRoomLeft = null;
            OnMasterSwitched = null;
            OnRoomCustomPropertiesChanged = null;
            OnRoomSystemPropertiesChanged = null;
            OnPlayerCustomPropertiesChanged = null;
            OnPlayerActivityChanged = null;
            OnCustomEvent = null;
            OnRoomKicked = null;
            OnDisconnected = null;
            OnError = null;

            LCLogger.Debug("Client close ...");
            if (lobby != null) {
                LCLogger.Debug("Client lobby close ...");
                await lobby.Close();
                lobby = null;
            }
            if (Room != null) {
                LCLogger.Debug("Client room close ...");
                await Room.Close();
                Room = null;
            }
        }
    }
}
