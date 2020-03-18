using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    public class LCIMVideoMessage : LCIMFileMessage {
        public double Duration {
            get {
                if (double.TryParse(File.MetaData["duration"] as string, out double duration)) {
                    return duration;
                }
                return 0;
            }
        }

        public LCIMVideoMessage(LCFile file) : base(file) {

        }
    }
}
