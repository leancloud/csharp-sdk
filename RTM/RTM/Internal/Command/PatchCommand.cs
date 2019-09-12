using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal class PatchCommand : AVIMCommand
    {

        internal struct Patch
        {
            public string MessageId { get; set; }
            public string ConvId { get; set; }
            public string From { get; set; }
            public long MetaTimestamp { get; set; }
            public long PatchTimestamp { get; set; }
            public string PatchData { get; set; }
            public bool Recall { get; set; }
            public byte[] BinaryData { get; set; }
            public bool MentionAll { get; set; }
            public IEnumerable<string> MentionIds { get; set; }

            public IDictionary<string, object> Encode()
            {
                return new Dictionary<string, object>()
                {
                    { "cid",this.ConvId},
                    { "mid",this.MessageId},
                    { "from",this.From},
                    { "timestamp",this.MetaTimestamp},
                    { "recall",this.Recall},
                    { "data",this.PatchData},
                    { "patchTimestamp",this.PatchTimestamp},
                    { "binaryMsg",this.BinaryData},
                    { "mentionAll",this.MentionAll},
                    { "meintonPids",this.MentionIds}
                } as IDictionary<string, object>;
            }
        }

        public PatchCommand()
            : base(cmd: "patch", op: "modify")
        {
            this.Patches = new List<Patch>();
        }

        public PatchCommand(AVIMCommand source, ICollection<Patch> sourcePatchs)
            : base(source: source)
        {
            this.Patches = sourcePatchs;
        }

        public ICollection<Patch> Patches { get; set; }

        public IList<IDictionary<string, object>> EncodePatches()
        {
            return this.Patches.Select(p => p.Encode().Trim()).ToList();
        }

        public PatchCommand Recall(IAVIMMessage message)
        {
            var patch = new Patch()
            {
                ConvId = message.ConversationId,
                From = message.FromClientId,
                MessageId = message.Id,
                MetaTimestamp = message.ServerTimestamp,
                Recall = true,
                PatchTimestamp = DateTime.Now.ToUnixTimeStamp()
            };

            this.Patches.Add(patch);
            this.Argument("patches", this.EncodePatches());
            return new PatchCommand(this, this.Patches);
        }

        public PatchCommand Modify(IAVIMMessage oldMessage, IAVIMMessage newMessage)
        {
            var patch = new Patch()
            {
                ConvId = oldMessage.ConversationId,
                From = oldMessage.FromClientId,
                MessageId = oldMessage.Id,
                MetaTimestamp = oldMessage.ServerTimestamp,
                Recall = false,
                PatchTimestamp = DateTime.Now.ToUnixTimeStamp(),
                PatchData = newMessage.Serialize()
            };

            this.Patches.Add(patch);
            this.Argument("patches", this.EncodePatches());
            return new PatchCommand(this, this.Patches);
        }
    }
}
