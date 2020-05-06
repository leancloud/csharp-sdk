using System;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Object {
    internal class LCObjectData {
        internal string ClassName {
            get; set;
        }

        internal string ObjectId {
            get; set;
        }

        internal DateTime CreatedAt {
            get; set;
        }

        internal DateTime UpdatedAt {
            get; set;
        }

        internal Dictionary<string, object> CustomPropertyDict;

        internal LCObjectData() {
            CustomPropertyDict = new Dictionary<string, object>();
        }

        internal static LCObjectData Decode(IDictionary dict) {
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
                    objectData.CreatedAt = createdAt;
                } else if (key == "updatedAt" && DateTime.TryParse(value.ToString(), out DateTime updatedAt)) {
                    objectData.UpdatedAt = updatedAt;
                } else {
                    objectData.CustomPropertyDict[key] = LCDecoder.Decode(value);
                }
            }
            return objectData;
        }

        internal static Dictionary<string, object> Encode(LCObjectData objectData) {
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
                dict["createdAt"] = objectData.CreatedAt;
            }
            if (objectData.UpdatedAt != null) {
                dict["updatedAt"] = objectData.UpdatedAt;
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
