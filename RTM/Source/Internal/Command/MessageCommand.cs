using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal class MessageCommand : AVIMCommand
    {
        public MessageCommand()
            : base(cmd: "direct")
        {

        }

        public MessageCommand(AVIMCommand source)
            : base(source: source)
        {

        }

        public MessageCommand ConvId(string convId)
        {
            return new MessageCommand(this.Argument("cid", convId));
        }

        public MessageCommand Receipt(bool receipt)
        {
            return new MessageCommand(this.Argument("r", receipt));
        }

        public MessageCommand Transient(bool transient)
        {
            if (transient) return new MessageCommand(this.Argument("transient", transient));
            return new MessageCommand(this);
        }
        public MessageCommand Priority(int priority)
        {
            if (priority > 1) return new MessageCommand(this.Argument("level", priority));
            return new MessageCommand(this);
        }
        public MessageCommand Will(bool will)
        {
            if (will) return new MessageCommand(this.Argument("will", will));
            return new MessageCommand(this);
        }
        public MessageCommand Distinct(string token)
        {
            return new MessageCommand(this.Argument("dt", token));
        }
        public MessageCommand Message(string msg)
        {
            return new MessageCommand(this.Argument("msg", msg));
        }
        public MessageCommand BinaryEncode(bool binaryEncode)
        {
            return new MessageCommand(this.Argument("bin", binaryEncode));
        }

        public MessageCommand PushData(IDictionary<string, object> pushData)
        {
            return new MessageCommand(this.Argument("pushData", Json.Encode(pushData)));
        }

        public MessageCommand Mention(IEnumerable<string> clientIds)
        {
            var mentionedMembers = clientIds.ToList();
            return new MessageCommand(this.Argument("mentionPids", mentionedMembers));
        }

        public MessageCommand MentionAll(bool mentionAll)
        {
			return new MessageCommand(this.Argument("mentionAll", mentionAll));
        }

        public MessageCommand Binary(byte[] data)
        {
            return new MessageCommand(this.Argument("binaryMsg", data));
        }
    }
}
