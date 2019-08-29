using LeanCloud;

namespace LeanCloud.Storage.Internal {
    public class RTMCommand : AVCommand {
        public override string Server => AVClient.CurrentConfiguration.RTMServer;
    }
}
