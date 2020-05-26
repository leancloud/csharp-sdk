using System;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Object {
    public class LCObjectData {
        public string ClassName {
            get; set;
        }

        public string ObjectId {
            get; set;
        }

        public DateTime CreatedAt {
            get; set;
        }

        public DateTime UpdatedAt {
            get; set;
        }

        public Dictionary<string, object> CustomPropertyDict;

        public LCObjectData() {
            CustomPropertyDict = new Dictionary<string, object>();
        }

        public static LCObjectData Decode(IDictionary dict) {
            if (dict == null) {
                return null;
            }
            LCObjectData objectData = new LCObjectData();
            foreach (DictionaryEntry kv in dict) {
                string key = kv.Key.ToString();
                object value = kv.Value;
                if (key == "className") {
                    objectData.ClassName = value.ToString();
                } else if (key == "objectId") {
                    objectData.ObjectId = value.ToString();
                } else if (key == "createdAt" && DateTime.TryParse(value.ToString(), out DateTime createdAt)) {
                    objectData.CreatedAt = createdAt.ToLocalTime();
                } else if (key == "updatedAt" && DateTime.TryParse(value.ToString(), out DateTime updatedAt)) {
                    objectData.UpdatedAt = updatedAt.ToLocalTime();
                } else {
                    objectData.CustomPropertyDict[key] = LCDecoder.Decode(value);
                }
            }
            return objectData;
        }

        public static Dictionary<string, object> Encode(LCObjectData objectData) {
            if (objectData == null) {
                return null;
            }
            Dictionary<string, object> dict = new Dictionary<string, object> {
                { "className", objectData.ClassName }
            };
            if (!string.IsNullOrEmpty(objectData.ObjectId)) {
                dict["objectId"] = objectData.ObjectId;
            }
            if (objectData.CreatedAt != null) {
                dict["createdAt"] = objectData.CreatedAt.ToUniversalTime();
            }
            if (objectData.UpdatedAt != null) {
                dict["updatedAt"] = objectData.UpdatedAt.ToUniversalTime();
            }
            if (objectData.CustomPropertyDict != null) {
                foreach (KeyValuePair<string, object> kv in objectData.CustomPropertyDict) {
                    string key = kv.Key;
                    object value = kv.Value;
                    dict[key] = LCEncoder.Encode(value);
                }
            }
            return dict;
        }
    }
}
