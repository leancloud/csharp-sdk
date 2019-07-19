using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal class AckCommand : AVIMCommand
    {
        public AckCommand()
            : base(cmd: "ack")
        {

        }

        public AckCommand(AVIMCommand source)
            : base(source)
        {

        }

        public AckCommand Message(IAVIMMessage message)
        {
            return new AckCommand()
                .ConversationId(message.ConversationId)
                .MessageId(message.Id);
        }

        public AckCommand MessageId(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                messageId = "";
            }
            return new AckCommand(this.Argument("mid", messageId));
        }

        public AckCommand ConversationId(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                conversationId = "";
            }
            return new AckCommand(this.Argument("cid", conversationId));
        }

        public AckCommand FromTimeStamp(long startTimeStamp)
        {
            return new AckCommand(this.Argument("fromts", startTimeStamp));
        }

        public AckCommand ToTimeStamp(long endTimeStamp)
        {
            return new AckCommand(this.Argument("tots", endTimeStamp));
        }

        public AckCommand ReadAck()
        {
            return new AckCommand(this.Argument("read", true));
        }
    }
}
