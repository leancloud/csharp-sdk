using System;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Object;

namespace LeanCloud.Storage.Internal.Codec {
    internal static class LCDecoder {
        internal static object Decode(object obj) {
            if (obj is IDictionary dict) {
                if (dict.Contains("__type")) {
                    string type = dict["__type"].ToString();
                    if (type == "Date") {
                        return DecodeDate(dict);
                    } else if (type == "Bytes") {
                        return DecodeBytes(dict);
                    } else if (type == "Object") {
                        return DecodeObject(dict);
                    } else if (type == "Pointer") {
                        return DecodeObject(dict);
                    } else if (type == "Relation") {
                        return DecodeRelation(dict);
                    } else if (type == "GeoPoint") {
                        return DecodeGeoPoint(dict);
                    }
                }
                Dictionary<string, object> d = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> kv in dict) {
                    string key = kv.Key;
                    object value = kv.Value;
                    d[key] = Decode(value); 
                }
                return d;
            } else if (obj is IList list) {
                List<object> l = new List<object>();
                foreach (object o in list) {
                    object v = Decode(o);
                    l.Add(v);
                }
            }
            return obj;
        }

        static DateTime DecodeDate(IDictionary dict) {
            string str = dict["iso"].ToString();
            DateTime dateTime = DateTime.Parse(str);
            return dateTime.ToLocalTime();
        }

        static byte[] DecodeBytes(IDictionary dict) {
            string str = dict["base64"].ToString();
            byte[] bytes = Convert.FromBase64String(str);
            return bytes;
        }

        static LCObject DecodeObject(IDictionary dict) {
            string className = dict["className"].ToString();
            LCObject obj = LCObject.Create(className);
            LCObjectData objectData = LCObjectData.Decode(dict as Dictionary<string, object>);
            // TODO merge

            return obj;
        }

        static LCRelation<LCObject> DecodeRelation(IDictionary dict) {
            LCRelation<LCObject> relation = new LCRelation<LCObject>();
            relation.targetClass = dict["className"].ToString();
            return relation;
        }

        static LCGeoPoint DecodeGeoPoint(IDictionary data) {
            double latitude = double.Parse(data["latitude"].ToString());
            double longitude = double.Parse(data["longitude"].ToString());
            LCGeoPoint geoPoint = new LCGeoPoint(latitude, longitude);
            return geoPoint;
        }
    }
}
