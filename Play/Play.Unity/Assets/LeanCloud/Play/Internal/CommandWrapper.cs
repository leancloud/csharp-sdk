using LeanCloud.Play.Protocol;

namespace LeanCloud.Play {
    internal class CommandWrapper {
        internal CommandType Cmd {
            get; set;
        }

        internal OpType Op {
            get; set;
        }

        internal Body Body {
            get; set;
        }
    }
}
