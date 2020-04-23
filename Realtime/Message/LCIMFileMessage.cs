using System;
using System.Collections.Generic;
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
                if (File.MetaData.TryGetValue("format", out object format)) {
                    return format as string;
                }
                return "unknown/unknown";
            }
        }

        public string Url {
            get {
                return File.Url;
            }
        }

        internal LCIMFileMessage() : base() {

        }

        public LCIMFileMessage(LCFile file) : base() {
            File = file;
        }

        internal override int MessageType => FileMessageType;

        internal override Dictionary<string, object> Encode() {
            if (File == null) {
                throw new Exception("File MUST NOT be null before sent.");
            }
            Dictionary<string, object> fileData = new Dictionary<string, object> {
                { "objId", File.ObjectId },
                { "url", File.Url },
                { "metaData", new Dictionary<string, object> {
                    { "name", File.Name },
                    { "format", File.MimeType }
                } }
            };
            if (File.MetaData.TryGetValue("size", out object size)) {
                Dictionary<string, object> metaData = fileData["metaData"] as Dictionary<string, object>;
                metaData["size"] = size;
            }
            Dictionary<string, object> data = base.Encode();
            data["_lcfile"] = fileData;
            return data;
        }

        protected override void DecodeMessageData(Dictionary<string, object> msgData) {
            base.DecodeMessageData(msgData);
            Dictionary<string, object> fileData = msgData["_lcfile"] as Dictionary<string, object>;
            string objectId = fileData["objId"] as string;
            File = LCObject.CreateWithoutData(LCFile.CLASS_NAME, objectId) as LCFile;
            if (fileData.TryGetValue("url", out object url)) {
                File.Url = url as string;
            }
            if (fileData.TryGetValue("metaData", out object metaData)) {
                File.MetaData = metaData as Dictionary<string, object>;
                if (File.MetaData.TryGetValue("name", out object name)) {
                    File.Name = name as string;
                }
            }
        }
    }
}
