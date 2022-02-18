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
                } else if (key == "createdAt" && value is DateTime createdAt) {
                    objectData.CreatedAt = createdAt;
                } else if (key == "updatedAt" && value is DateTime updatedAt) {
                    objectData.UpdatedAt = updatedAt;
                } else {
                    if (key == "ACL" &&
                        value is Dictionary<string, object> dic) {
                        objectData.CustomPropertyDict[key] = LCDecoder.DecodeACL(dic);
                    } else {
                        objectData.CustomPropertyDict[key] = LCDecoder.Decode(value);
                    }
                }
            }
            return objectData;
        }
    }
}
