using System;
using System.IO;
using System.Collections.Generic;
using LeanCloud.Storage;
using System.Threading.Tasks;

namespace LeanCloud.Realtime {
    public class LCIMFileMessage : LCIMTextMessage {
        public LCFile File {
            get; set;
        }

        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        public int Size {
            get; private set;
        }

        /// <summary>
        /// The format extension of the file. 
        /// </summary>
        public string Format {
            get; private set;
        }

        /// <summary>
        /// The URL of the file.
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
            if (string.IsNullOrEmpty(File.ObjectId)) {
                throw new Exception("File MUST be saved before sent.");
            }
            Dictionary<string, object> fileData = new Dictionary<string, object> {
                { MessageDataObjectIdKey, File.ObjectId }
            };
            // 链接
            if (!string.IsNullOrEmpty(File.Url)) {
                fileData[MessageDataUrlKey] = File.Url;
            }
            // 元数据
            Dictionary<string, object> metaData = new Dictionary<string, object>();
            // 文件名
            if (!string.IsNullOrEmpty(File.Name)) {
                metaData[MessageDataMetaNameKey] = File.Name;
            }
            // 文件扩展名
            string format = null;
            if (File.MetaData != null &&
                File.MetaData.TryGetValue(MessageDataMetaFormatKey, out object f)) {
                // 优先使用用户设置值
                format = f as string;
            } else if (File.Name != null &&
                !string.IsNullOrEmpty(Path.GetExtension(File.Name))) {
                // 根据文件名推测
                format = Path.GetExtension(File.Name)?.Replace(".", string.Empty);
            } else if (File.Url != null &&
                !string.IsNullOrEmpty(Path.GetExtension(File.Url))) {
                // 根据 url 推测
                format = Path.GetExtension(File.Url)?.Replace(".", string.Empty);
            }
            if (!string.IsNullOrEmpty(format)) {
                metaData[MessageDataMetaFormatKey] = format;
            }
            // 文件大小
            if (File.MetaData != null &&
                File.MetaData.TryGetValue(MessageDataMetaSizeKey, out object size)) {
                metaData[MessageDataMetaSizeKey] = size;
            }
            fileData[MessageDataMetaDataKey] = metaData;

            Dictionary<string, object> data = base.Encode();
            data[MessageFileKey] = fileData;
            return data;
        }

        internal override void Decode(Dictionary<string, object> msgData) {
            base.Decode(msgData);

            if (msgData.TryGetValue(MessageFileKey, out object fileDataObject)) {
                Dictionary<string, object> fileData = fileDataObject as Dictionary<string, object>;
                if (fileData == null) {
                    return;
                }
                if (fileData.TryGetValue(MessageDataObjectIdKey, out object objectIdObject)) {
                    string objectId = objectIdObject as string;
                    File = LCObject.CreateWithoutData(LCFile.CLASS_NAME, objectId) as LCFile;
                    if (fileData.TryGetValue(MessageDataUrlKey, out object url)) {
                        File.Url = url as string;
                    }
                    if (fileData.TryGetValue(MessageDataMetaDataKey, out object metaData)) {
                        File.MetaData = metaData as Dictionary<string, object>;
                        if (File.MetaData == null) {
                            return;
                        }
                        if (File.MetaData.TryGetValue(MessageDataMetaNameKey, out object name)) {
                            File.Name = name as string;
                        }
                        if (File.MetaData.TryGetValue(MessageDataMetaSizeKey, out object size) &&
                            int.TryParse(size as string, out int s)) {
                            Size = s;
                        }
                        if (File.MetaData.TryGetValue(MessageDataMetaFormatKey, out object format)) {
                            Format = format as string;
                        }
                    }
                }
            }
        }

        internal override async Task PrepareSend() {
            if (File != null && string.IsNullOrEmpty(File.ObjectId)) {
                await File.Save();
            }
        }
    }
}
