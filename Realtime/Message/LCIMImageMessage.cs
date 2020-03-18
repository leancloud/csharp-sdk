using System;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    public class LCIMImageMessage : LCIMFileMessage {
        public int Width {
            get {
                if (int.TryParse(File.MetaData["width"] as string, out int width)) {
                    return width;
                }
                return 0;
            }
        }

        public int Height {
            get {
                if (int.TryParse(File.MetaData["height"] as string, out int height)) {
                    return height;
                }
                return 0;
            }
        }

        public LCIMImageMessage(LCFile file) : base(file) {
        }
    }
}
