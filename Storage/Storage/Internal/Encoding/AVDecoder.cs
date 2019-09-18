using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using LeanCloud.Utilities;

namespace LeanCloud.Storage.Internal {
    public class AVDecoder {
        // This class isn't really a Singleton, but since it has no state, it's more efficient to get
        // the default instance.
        private static readonly AVDecoder instance = new AVDecoder();
        public static AVDecoder Instance {
            get {
                return instance;
            }
        }

        // Prevent default constructor.
        private AVDecoder() { }

        public object Decode(object data) {
            // 如果是字典类型
            if (data is IDictionary<string, object> dict) {
                if (dict.ContainsKey("__op")) {
                    return AVFieldOperations.Decode(dict);
                }
                if (dict.TryGetValue("__type", out object type)) {
                    string typeString = type as string;
                    switch (typeString) {
                        case "Date":
                            return ParseDate(dict["iso"] as string);
                        case "Bytes":
                            return Convert.FromBase64String(dict["base64"] as string);
                        case "Pointer": {
                                if (dict.Keys.Count > 3) {
                                    return DecodeAVObject(dict);
                                }
                                return DecodePointer(dict["className"] as string, dict["objectId"] as string);
                            }
                        case "GeoPoint":
                            return new AVGeoPoint(Conversion.To<double>(dict["latitude"]),
                                Conversion.To<double>(dict["longitude"]));
                        case "Object":
                            return DecodeAVObject(dict);
                        case "Relation":
                            return AVRelationBase.CreateRelation(null, null, dict["className"] as string);
                        default:
                            break;
                    }
                }
                var converted = new Dictionary<string, object>();
                foreach (var pair in dict) {
                    converted[pair.Key] = Decode(pair.Value);
                }
                return converted;
            }
            // 如果是数组类型
            if (data is IList<object> list) {
                return (from item in list
                        select Decode(item)).ToList();
            }
            // 原样返回
            return data;
        }

        protected virtual object DecodePointer(string className, string objectId) {
            return AVObject.CreateWithoutData(className, objectId);
        }

        protected virtual object DecodeAVObject(IDictionary<string, object> dict) {
            var className = dict["className"] as string;
            var state = AVObjectCoder.Instance.Decode(dict, this);
            return AVObject.FromState<AVObject>(state, className);
        }

        public virtual IList<T> DecodeList<T>(object data) {
            IList<T> rtn = null;
            var list = (IList<object>)data;
            if (list != null) {
                rtn = new List<T>();
                foreach (var item in list) {
                    rtn.Add((T)item);
                }
            }
            return rtn;
        }

        public static DateTime ParseDate(string input) {
            return DateTime.ParseExact(input,
              AVClient.DateFormatStrings,
              CultureInfo.InvariantCulture,
              DateTimeStyles.AssumeUniversal);
        }
    }
}
