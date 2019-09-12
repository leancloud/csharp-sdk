using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal class ReadCommand : AVIMCommand
    {
        internal class ConvRead
        {
            internal string ConvId { get; set; }
            internal string MessageId { get; set; }
            internal long Timestamp { get; set; }
            public override bool Equals(object obj)
            {
                ConvRead cr = obj as ConvRead;
                return cr.ConvId == this.ConvId;
            }
            public override int GetHashCode()
            {
                return this.ConvId.GetHashCode() ^ this.MessageId.GetHashCode() ^ this.Timestamp.GetHashCode();
            }
        }

        public ReadCommand()
            : base(cmd: "read")
        {

        }

        public ReadCommand(AVIMCommand source)
            : base(source)
        {

        }

        public ReadCommand ConvId(string convId)
        {
            return new ReadCommand(this.Argument("cid", convId));
        }

        public ReadCommand ConvIds(IEnumerable<string> convIds)
        {
            if (convIds != null)
            {
                if (convIds.Count() > 0)
                {
                    return new ReadCommand(this.Argument("cids", convIds.ToList()));
                }
            }
            return this;

        }

        public ReadCommand Conv(ConvRead conv)
        {
            return Convs(new ConvRead[] { conv });
        }

        public ReadCommand Convs(IEnumerable<ConvRead> convReads)
        {
            if (convReads != null)
            {
                if (convReads.Count() > 0)
                {
                    IList<IDictionary<string, object>> payload = new List<IDictionary<string, object>>();

                    foreach (var convRead in convReads)
                    {
                        var convDic = new Dictionary<string, object>();
                        convDic.Add("cid", convRead.ConvId);
                        if (!string.IsNullOrEmpty(convRead.MessageId))
                        {
                            convDic.Add("mid", convRead.MessageId);
                        }
                        if (convRead.Timestamp != 0)
                        {
                            convDic.Add("timestamp", convRead.Timestamp);
                        }
                        payload.Add(convDic);
                    }

                    return new ReadCommand(this.Argument("convs", payload));
                }
            }
            return this;
        }
    }
}
