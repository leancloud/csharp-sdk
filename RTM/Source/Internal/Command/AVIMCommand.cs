using LeanCloud;
using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    /// <summary>
    /// Command.
    /// </summary>
    public class AVIMCommand
    {
        protected readonly string cmd;
        protected readonly string op;
        protected string appId;
        protected string peerId;
        protected AVIMSignature signature;
        protected readonly IDictionary<string, object> arguments;

        public int TimeoutInSeconds { get; set; }

        protected readonly IDictionary<string, object> estimatedData = new Dictionary<string, object>();
        internal readonly object mutex = new object();
        internal static readonly object Mutex = new object();

        public AVIMCommand() :
            this(arguments: new Dictionary<string, object>())
        {

        }
        protected AVIMCommand(string cmd = null,
            string op = null,
            string appId = null,
            string peerId = null,
            AVIMSignature signature = null,
            IDictionary<string, object> arguments = null)
        {
            this.cmd = cmd;
            this.op = op;
            this.arguments = arguments == null ? new Dictionary<string, object>() : arguments;
            this.peerId = peerId;
            this.signature = signature;
        }

        protected AVIMCommand(AVIMCommand source,
            string cmd = null,
            string op = null,
            string appId = null,
            string peerId = null,
            IDictionary<string, object> arguments = null,
            AVIMSignature signature = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source", "Source can not be null");
            }
            this.cmd = source.cmd;
            this.op = source.op;
            this.arguments = source.arguments;
            this.peerId = source.peerId;
            this.appId = source.appId;
            this.signature = source.signature;

            if (cmd != null)
            {
                this.cmd = cmd;
            }
            if (op != null)
            {
                this.op = op;
            }
            if (arguments != null)
            {
                this.arguments = arguments;
            }
            if (peerId != null)
            {
                this.peerId = peerId;
            }
            if (appId != null)
            {
                this.appId = appId;
            }
            if (signature != null)
            {
                this.signature = signature;
            }
        }

        public AVIMCommand Command(string cmd)
        {
            return new AVIMCommand(this, cmd: cmd);
        }
        public AVIMCommand Option(string op)
        {
            return new AVIMCommand(this, op: op);
        }
        public AVIMCommand Argument(string key, object value)
        {
            lock (mutex)
            {
                this.arguments[key] = value;
                return new AVIMCommand(this);
            }
        }
        public AVIMCommand AppId(string appId)
        {
            this.appId = appId;
            return new AVIMCommand(this, appId: appId);
        }

        public AVIMCommand PeerId(string peerId)
        {
            this.peerId = peerId;
            return new AVIMCommand(this, peerId: peerId);
        }

        public AVIMCommand IDlize()
        {
            this.Argument("i", AVIMCommand.NextCmdId);
            return this;
        }

        public virtual IDictionary<string, object> Encode()
        {
            lock (mutex)
            {
                estimatedData.Clear();
                estimatedData.Merge(arguments);
                estimatedData.Add("cmd", cmd);
                estimatedData.Add("appId", this.appId);
                if (!string.IsNullOrEmpty(op))
                    estimatedData.Add("op", op);
                if (!string.IsNullOrEmpty(peerId))
                    estimatedData.Add("peerId", peerId);

                return estimatedData;
            }
        }

        public virtual string EncodeJsonString()
        {
            var json = this.Encode();
            return Json.Encode(json);
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(this.cmd);
            }
        }

        private static Int32 lastCmdId = -65536;
        internal static Int32 NextCmdId
        {
            get
            {
                lock (Mutex)
                {
                    lastCmdId++;

                    if (lastCmdId > ushort.MaxValue)
                    {
                        lastCmdId = -65536;
                    }
                    return lastCmdId;
                }
            }
        }
    }
}
