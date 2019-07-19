using System;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 强制被踢下线处理
    /// </summary>
    internal class GoAwayListener : IAVIMListener {
        Action onGoAway;

        public event Action OnGoAway { 
            add { 
                onGoAway += value;
            }
            remove { 
                onGoAway -= value;
            }
        }

        public void OnNoticeReceived(AVIMNotice notice) {
            // TODO 退出并清理路由缓存
            onGoAway?.Invoke();
        }

        public bool ProtocolHook(AVIMNotice notice) {
            return notice.CommandName == "goaway";
        }
    }
}
