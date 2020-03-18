using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    public class LCIMFileMessage : LCIMTextMessage {
        public LCFile File {
            get; set;
        }

        public int Size {
            get {
                if (int.TryParse(File.MetaData["size"] as string, out int size)) {
                    return size;
                }
                return 0;
            }
        }

        public string Format {
            get {
                return File.MimeType;
            }
        }

        public string Url {
            get {
                return File.Url;
            }
        }

        public LCIMFileMessage(LCFile file) : base(null) {
            File = file;
        }

        internal override string Serialize() {
            if (File == null) {
                throw new Exception("File MUST NOT be null before sent.");
            }
            File.MetaData["name"] = File.Name;
            File.MetaData["format"] = File.MimeType;
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "objId", File.ObjectId },
                { "url", File.Url },
                { "metaData", File.MetaData }
            };
            return JsonConvert.SerializeObject(new Dictionary<string, object> {
                { "_lcfile", data }
            });
        }
    }
}
