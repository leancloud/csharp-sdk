using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal.File;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Common;

namespace LeanCloud.Storage {
    public class LCFile : LCObject {
        public const string CLASS_NAME = "_File";

        public string Name {
            get {
                return this["name"] as string;
            } set {
                this["name"] = value;
            }
        }

        public string MimeType {
            get {
                return this["mime_type"] as string;
            } set {
                this["mime_type"] = value;
            }
        }

        public string Url {
            get {
                return this["url"] as string;
            } set {
                this["url"] = value;
            }
        }

        public Dictionary<string, object> MetaData {
            get {
                return this["metaData"] as Dictionary<string, object>;
            } set {
                this["metaData"] = value;
            }
        }

        readonly byte[] data;

        public LCFile() : base(CLASS_NAME) {
            MetaData = new Dictionary<string, object>();
        }

        public LCFile(string name, byte[] bytes) : this() {
            Name = name;
            data = bytes;
        }

        public LCFile(string name, string path) : this() {
            Name = name;
            MimeType = LCMimeTypeMap.GetMimeType(path);
            data = File.ReadAllBytes(path);
        }

        public LCFile(string name, Uri url) : this() {
            Name = name;
            Url = url.AbsoluteUri;
        }

        public void AddMetaData(string key, object value) {
            MetaData[key] = value;
        }

        public async Task<LCFile> Save(Action<long, long> onProgress = null) {
            if (!string.IsNullOrEmpty(Url)) {
                // 外链方式
                await base.Save();
            } else {
                // 上传文件
                Dictionary<string, object> uploadToken = await GetUploadToken();
                string uploadUrl = uploadToken["upload_url"] as string;
                string key = uploadToken["key"] as string;
                string token = uploadToken["token"] as string;
                string provider = uploadToken["provider"] as string;
                try {
                    if (provider == "s3") {
                        // AWS
                        LCAWSUploader uploader = new LCAWSUploader(uploadUrl, MimeType, data);
                        await uploader.Upload(onProgress);
                    } else if (provider == "qiniu") {
                        // Qiniu
                        LCQiniuUploader uploader = new LCQiniuUploader(uploadUrl, token, key, data);
                        await uploader.Upload(onProgress);
                    } else {
                        throw new Exception($"{provider} is not support.");
                    }
                    LCObjectData objectData = LCObjectData.Decode(uploadToken);
                    Merge(objectData);
                    _ = LCApplication.HttpClient.Post<Dictionary<string, object>>("fileCallback", data: new Dictionary<string, object> {
                        { "result", true },
                        { "token", token }
                    });
                } catch (Exception e) {
                    _ = LCApplication.HttpClient.Post<Dictionary<string, object>>("fileCallback", data: new Dictionary<string, object> {
                        { "result", false },
                        { "token", token }
                    });
                    throw e;
                }
            }
            return this;
        }

        public new async Task Delete() {
            if (string.IsNullOrEmpty(ObjectId)) {
                return;
            }
            string path = $"files/{ObjectId}";
            await LCApplication.HttpClient.Delete(path);
        }

        public string GetThumbnailUrl(int width, int height, int quality = 100, bool scaleToFit = true, string format = "png") {
            int mode = scaleToFit ? 2 : 1;
            return $"{Url}?imageView/{mode}/w/{width}/h/{height}/q/{quality}/format/{format}";
        }

        async Task<Dictionary<string, object>> GetUploadToken() {
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "name", Name },
                { "key", Guid.NewGuid().ToString() },
                { "__type", "File" },
                { "mime_type", MimeType },
                { "metaData", MetaData }
            };
            return await LCApplication.HttpClient.Post<Dictionary<string, object>>("fileTokens", data: data);
        }

        public static LCQuery<LCFile> GetQuery() {
            return new LCQuery<LCFile>(CLASS_NAME);
        }
    }
}
