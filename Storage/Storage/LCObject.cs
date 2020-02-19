using System.Collections.Generic;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage.Internal.Operation;

namespace LeanCloud.Storage {
    /// <summary>
    /// 对象类
    /// </summary>
    public class LCObject {
        /// <summary>
        /// 最近一次与服务端同步的数据
        /// </summary>
        LCObjectData data;

        /// <summary>
        /// 预算数据
        /// </summary>
        Dictionary<string, object> estimatedData;

        /// <summary>
        /// 操作字典
        /// </summary>
        Dictionary<string, LCOperation> operationDict;

        public string ClassName {
            get {
                return data.ClassName;
            }
        }

        public string ObjectId {
            get {
                return data.ObjectId;
            }
        }

        public LCObject() {
        }

        internal static LCObject Create(string className) {
            // TODO
            return null;
        }
    }
}
