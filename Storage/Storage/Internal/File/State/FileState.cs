using System;
using System.Collections.Generic;
using System.IO;

namespace LeanCloud.Storage.Internal {
    public class FileState {
        public string ObjectId { get; internal set; }
        public string Name { get; internal set; }
        public string CloudName { get; set; }
        public string MimeType { get; internal set; }
        public Uri Url { get; internal set; }
        public IDictionary<string, object> MetaData { get; internal set; }
        public long Size { get; internal set; }
        public long FixedChunkSize { get; internal set; }

        public int counter;
        public Stream frozenData;
        public string bucketId;
        public string bucket;
        public string token;
        public long completed;
        public List<string> block_ctxes = new List<string>();

        public static FileState NewFromDict(IDictionary<string, object> dict) {
            FileState state = new FileState {
                ObjectId = dict["objectId"] as string,
                Url = new Uri(dict["url"] as string, UriKind.Absolute)
            };
            if (dict.TryGetValue("name", out object name)) {
                state.Name = name as string;
            }
            if (dict.TryGetValue("metaData", out object metaData)) {
                state.MetaData = metaData as Dictionary<string, object>;
            }
            return state;
        }
    }
}
