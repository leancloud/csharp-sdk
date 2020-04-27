using System;
using System.Collections.Generic;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 文件消息
    /// </summary>
    public class LCIMFileMessage : LCIMTextMessage {
        /// <summary>
        /// 文件
        /// </summary>
        public LCFile File {
            get; set;
        }

        /// <summary>
        /// 文件大小
        /// </summary>
        public int Size {
            get {
                if (int.TryParse(File.MetaData[MessageDataMetaSizeKey] as string, out int size)) {
                    return size;
                }
                return 0;
            }
        }

        /// <summary>
        /// 文件类型
        /// </summary>
        public string Format {
            get {
                return File.MimeType;
            }
        }

        /// <summary>
        /// 文件链接
        /// </summary>
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

        public override int MessageType => FileMessageType;

        internal override Dictionary<string, object> Encode() {
            if (File == null) {
                throw new Exception("File MUST NOT be null before sent.");
            }
            Dictionary<string, object> fileData = new Dictionary<string, object> {
                { MessageDataObjectIdKey, File.ObjectId },
                { MessageDataUrlKey, File.Url },
                { MessageDataMetaDataKey, new Dictionary<string, object> {
                    { MessageDataMetaNameKey, File.Name },
                    { MessageDataMetaFormatKey, File.MimeType }
                } }
            };
            if (File.MetaData.TryGetValue(MessageDataMetaSizeKey, out object size)) {
                Dictionary<string, object> metaData = fileData[MessageDataMetaDataKey] as Dictionary<string, object>;
                metaData[MessageDataMetaSizeKey] = size;
            }
            Dictionary<string, object> data = base.Encode();
            data[MessageFileKey] = fileData;
            return data;
        }

        internal override void Decode(Dictionary<string, object> msgData) {
            base.Decode(msgData);

            if (msgData.TryGetValue(MessageFileKey, out object fileDataObject)) {
                Dictionary<string, object> fileData = fileDataObject as Dictionary<string, object>;
                if (fileData.TryGetValue(MessageDataObjectIdKey, out object objectIdObject)) {
                    string objectId = objectIdObject as string;
                    File = LCObject.CreateWithoutData(LCFile.CLASS_NAME, objectId) as LCFile;
                    if (fileData.TryGetValue(MessageDataUrlKey, out object url)) {
                        File.Url = url as string;
                    }
                    if (fileData.TryGetValue(MessageDataMetaDataKey, out object metaData)) {
                        File.MetaData = metaData as Dictionary<string, object>;
                        if (File.MetaData.TryGetValue(MessageDataMetaNameKey, out object name)) {
                            File.Name = name as string;
                        }
                    }
                }
            }
        }
    }
}
