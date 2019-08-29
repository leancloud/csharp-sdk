using System;

namespace LeanCloud.Storage.Internal {
    public class EngineCommand : AVCommand {
        public override string Server => AVClient.CurrentConfiguration.EngineServer;
    }
}
