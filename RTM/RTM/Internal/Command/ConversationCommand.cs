using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal class ConversationCommand : AVIMCommand
    {
        protected IList<string> members;
        public ConversationCommand()
            : base(cmd: "conv")
        {

        }

        public ConversationCommand(AVIMCommand source)
            : base(source: source)
        {
        }

        public ConversationCommand Member(string clientId)
        {
            if (members == null)
            {
                members = new List<string>();
            }
            members.Add(clientId);
            return Members(members);
        }

        public ConversationCommand Members(IEnumerable<string> members)
        {
            this.members = members.ToList();
            return new ConversationCommand(this.Argument("m", members));
        }

        public ConversationCommand Transient(bool isTransient)
        {
            return new ConversationCommand(this.Argument("transient", isTransient));
        }

        public ConversationCommand Unique(bool isUnique)
        {
            return new ConversationCommand(this.Argument("unique", isUnique));
        }

        public ConversationCommand Temporary(bool isTemporary)
        {
            return new ConversationCommand(this.Argument("tempConv", isTemporary));
        }

        public ConversationCommand TempConvTTL(double tempConvTTL)
        {
            return new ConversationCommand(this.Argument("tempConvTTL", tempConvTTL));
        }

        public ConversationCommand Attr(IDictionary<string, object> attr)
        {
            return new ConversationCommand(this.Argument("attr", attr));
        }

        public ConversationCommand Set(string key, object value)
        {
            return new ConversationCommand(this.Argument(key, value));
        }

        public ConversationCommand ConversationId(string conversationId)
        {
            return new ConversationCommand(this.Argument("cid", conversationId));
        }

        public ConversationCommand Generate(AVIMConversation conversation)
        {
            var attr = conversation.EncodeAttributes();
            var cmd = new ConversationCommand()
                .ConversationId(conversation.ConversationId)
                .Attr(attr)
                .Members(conversation.MemberIds)
                .Transient(conversation.IsTransient)
                .Temporary(conversation.IsTemporary);

            if (conversation.IsTemporary)
            {
                var ttl = (conversation.expiredAt.Value - DateTime.Now).TotalSeconds;
                cmd = cmd.TempConvTTL(ttl);
            }

            return cmd;
        }

        public ConversationCommand Where(object encodedQueryString)
        {
            return new ConversationCommand(this.Argument("where", encodedQueryString));
        }

        public ConversationCommand Limit(int limit)
        {
            return new ConversationCommand(this.Argument("limit", limit));
        }

        public ConversationCommand Skip(int skip)
        {
            return new ConversationCommand(this.Argument("skip", skip));
        }

        public ConversationCommand Count()
        {
            return new ConversationCommand(this.Argument("count", 1));
        }

        public ConversationCommand Sort(string sort)
        {
            return new ConversationCommand(this.Argument("sort", sort));
        }

        public ConversationCommand TargetClientId(string targetClientId)
        {
            return new ConversationCommand(this.Argument("targetClientId", targetClientId));
        }

        public ConversationCommand QueryAllMembers(bool queryAllMembers)
        {
            return new ConversationCommand(this.Argument("queryAllMembers", queryAllMembers));
        }

    }
}
