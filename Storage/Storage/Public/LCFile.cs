using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Common;
using LeanCloud.Storage.Internal.File;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    /// <summary>
    /// LCFile is a local representation of a LeanCloud file.
    /// </summary>
    public class LCFile : LCObject {
        public const string CLASS_NAME = "_File";

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string Name {
            get {
                return this["name"] as string;
            } set {
                this["name"] = value;
            }
        }

        /// <summary>
        /// Gets the MIME type of the file.
        /// </summary>
        public string MimeType {
            get {
                return this["mime_type"] as string;
            } set {
                this["mime_type"] = value;
            }
        }

        /// <summary>
        /// Gets the url of the file.
        /// </summary>
        public string Url {
            get {
                return this["url"] as string;
            } set {
                this["url"] = value;
            }
        }

        /// <summary>
        /// Gets the metadata of the file.
        /// </summary>
        public Dictionary<string, object> MetaData {
            get {
                return this["metaData"] as Dictionary<string, object>;
            } set {
                this["metaData"] = value;
            }
        }

        /// <summary>
        /// Gets the path prefix of the file.
        /// </summary>
        public string PathPrefix {
            get; set;
        }

        readonly Stream stream;

        /// <summary>
        /// Creates a new file.
        /// </summary>
        public LCFile() : base(CLASS_NAME) {
        }

        /// <summary>
        /// Creates a new file from a byte array.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bytes"></param>
        public LCFile(string name, byte[] bytes) : this() {
            Name = name;
            stream = new MemoryStream(bytes);
        }

        /// <summary>
        /// Creates a new file from a local file.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        public LCFile(string name, string path) : this() {
            Name = name;
            MimeType = LCMimeTypeMap.GetMimeType(path);
            stream = new FileStream(path, FileMode.Open);
        }

        /// <summary>
        /// Creates a new external file from an url.
        /// The file content is saved externally, not copied to LeanCloud.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        public LCFile(string name, Uri url) : this() {
            Name = name;
            Url = url.AbsoluteUri;
        }

        /// <summary>
        /// Adds metadata.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddMetaData(string key, object value) {
            if (MetaData == null) {
                MetaData = new Dictionary<string, object>();
            }
            MetaData[key] = value;
        }

        /// <summary>
        /// Saves the file to LeanCloud.
        /// </summary>
        /// <param name="onProgress"></param>
        /// <returns></returns>
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
                        LCAWSUploader uploader = new LCAWSUploader(uploadUrl, MimeType, stream);
                        await uploader.Upload(onProgress);
                    } else if (provider == "qiniu") {
                        // Qiniu
                        string bucket = uploadToken["bucket"] as string;
                        LCQiniuUploader uploader = new LCQiniuUploader(uploadUrl, token, bucket, key, stream);
                        await uploader.Upload(onProgress);
                    } else {
                        throw new Exception($"{provider} is not support.");
                    }
                    LCObjectData objectData = LCObjectData.Decode(uploadToken);
                    Merge(objectData);
                    _ = LCCore.HttpClient.Post<Dictionary<string, object>>("fileCallback", data: new Dictionary<string, object> {
                        { "result", true },
                        { "token", token }
                    });
                } catch (Exception e) {
                    _ = LCCore.HttpClient.Post<Dictionary<string, object>>("fileCallback", data: new Dictionary<string, object> {
                        { "result", false },
                        { "token", token }
                    });
                    throw e;
                } finally {
                    stream?.Close();
                    stream?.Dispose();
                }
            }
            return this;
        }

        /// <summary>
        /// Deletes the file from LeanCloud.
        /// </summary>
        /// <returns></returns>
        public new async Task Delete() {
            if (string.IsNullOrEmpty(ObjectId)) {
                return;
            }
            string path = $"files/{ObjectId}";
            await LCCore.HttpClient.Delete(path);
        }

        /// <summary>
        /// Gets the thumbnail url of an image file.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="quality"></param>
        /// <param name="scaleToFit"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public string GetThumbnailUrl(int width, int height, int quality = 100, bool scaleToFit = true, string format = "png") {
            int mode = scaleToFit ? 2 : 1;
            return $"{Url}?imageView/{mode}/w/{width}/h/{height}/q/{quality}/format/{format}";
        }

        async Task<Dictionary<string, object>> GetUploadToken() {
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "name", Name },
                { "__type", "File" },
                { "mime_type", MimeType },
            };
            if (ACL != null) {
                data["ACL"] = LCEncoder.EncodeACL(ACL);
            }
            if (!string.IsNullOrEmpty(PathPrefix)) {
                data["prefix"] = PathPrefix;
                AddMetaData("prefix", PathPrefix);
            }
            data["metaData"] = MetaData;
            return await LCCore.HttpClient.Post<Dictionary<string, object>>("fileTokens", data: data);
        }

        /// <summary>
        /// Gets LCQuery of LCFile.
        /// </summary>
        /// <returns></returns>
        public static LCQuery<LCFile> GetQuery() {
            return new LCQuery<LCFile>(CLASS_NAME);
        }
    }
}
