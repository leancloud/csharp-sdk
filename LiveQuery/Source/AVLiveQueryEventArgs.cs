using System;
using System.Collections.Generic;
using LeanCloud;
using System.Collections;

namespace LeanCloud.LiveQuery
{
    /// <summary>
    /// AVLiveQuery 回调参数
    /// </summary>
    public class AVLiveQueryEventArgs<T> : EventArgs
        where T : AVObject
    {
        internal AVLiveQueryEventArgs()
        {

        }

        /// <summary>
        /// 更新事件
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// 更新字段
        /// </summary>
        public IEnumerable<string> Keys { get; set; }

        /// <summary>
        /// 更新数据
        /// </summary>
        public T Payload { get; set; }
    }
}
