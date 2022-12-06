using System.Collections.Generic;

namespace LeanCloud.Play {
    /// <summary>
    /// 创建房间标识
    /// </summary>
    public static class CreateRoomFlag {
        /// <summary>
        /// Master 掉线后固定 Master
        /// </summary>
        public const int FixedMaster = 1;
        /// <summary>
        /// 只允许 Master 设置房间属性
        /// </summary>
        public const int MasterUpdateRoomProperties = 2;
    }

    /// <summary>
    /// 创建房间选项
    /// </summary>
    public class RoomOptions {
        private const int MAX_PLAYER_COUNT = 10;

        /// <summary>
        /// 房间是否打开
        /// </summary>
        /// <value><c>true</c> if opened; otherwise, <c>false</c>.</value>
        public bool? Open {
            get; set;
        }

        /// <summary>
        /// 房间是否可见，只有「可见」的房间会出现在房间列表里
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public bool? Visible {
            get; set;
        }

        /// <summary>
        /// 房间为空后，延迟销毁的时间
        /// </summary>
        /// <value>The empty room ttl.</value>
        public int EmptyRoomTtl {
            get; set;
        }

        /// <summary>
        /// 玩家掉线后，延迟销毁的时间
        /// </summary>
        /// <value>The player ttl.</value>
        public int PlayerTtl {
            get; set;
        }

        /// <summary>
        /// 最大玩家数量
        /// </summary>
        /// <value>The max player count.</value>
        public int MaxPlayerCount {
            get; set;
        }

        /// <summary>
        /// 自定义房间属性
        /// </summary>
        /// <value>The custom room properties.</value>
        public PlayObject CustomRoomProperties {
            get; set;
        }

        /// <summary>
        /// 在大厅中可获得的房间属性「键」数组
        /// </summary>
        /// <value>The custo room property keys for lobby.</value>
        public List<string> CustomRoomPropertyKeysForLobby {
            get; set;
        }

        /// <summary>
        /// 创建房间标记，可多选
        /// </summary>
        /// <value>The flag.</value>
        public int Flag {
            get; set;
        }

        /// <summary>
        /// 房间插件名称
        /// </summary>
        /// <value>The name of the plugin.</value>
        public string PluginName {
            get; set;
        }

        /// <summary>
        /// RoomOptions 构造方法
        /// </summary>
        public RoomOptions() {
            Open = true;
            Visible = true;
            EmptyRoomTtl = 0;
            PlayerTtl = 0;
            MaxPlayerCount = MAX_PLAYER_COUNT;
            CustomRoomProperties = null;
            CustomRoomPropertyKeysForLobby = null;
            Flag = 0;
            PluginName = null;
        }
    }
}
