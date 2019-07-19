using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// Temporary conversation.
    /// </summary>
    public class AVIMTemporaryConversation : AVIMConversation
    {
        public DateTime ExpiredAt
        {
            get
            {
                if (expiredAt == null)
                    return DateTime.Now.AddDays(1);
                return expiredAt.Value;
            }

            set
            {
                expiredAt = value;
            }
        }

        internal AVIMTemporaryConversation(long ttl)
            : base(isTemporary: true)
        {
            this.expiredAt = DateTime.Now.AddDays(1);
        }
    }


}
