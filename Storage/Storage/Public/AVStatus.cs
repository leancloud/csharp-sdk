using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud
{
    /// <summary>
    /// 事件流系统中的一条状态
    /// </summary>
    [AVClassName("_Status")]
    public class AVStatus : AVObject
    {
        private static readonly HashSet<string> readOnlyKeys = new HashSet<string> {
            "messageId", "inboxType", "data","Source"
        };

        protected override bool IsKeyMutable(string key)
        {
            return !readOnlyKeys.Contains(key);
        }
    }
}
