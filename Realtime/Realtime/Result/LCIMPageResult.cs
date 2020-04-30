using System.Collections.ObjectModel;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 查询分页结果
    /// </summary>
    public class LCIMPageResult {
        /// <summary>
        /// 当前分页数据集
        /// </summary>
        public ReadOnlyCollection<string> Results {
            get; internal set;
        }

        /// <summary>
        /// 下次请求的数据
        /// </summary>
        public string Next {
            get; internal set;
        }
    }
}
