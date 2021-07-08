using System;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Operation;
using LeanCloud.Storage.Internal.Query;

namespace LeanCloud.Storage.Internal.Codec {
    public static class LCEncoder {
        public static readonly string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public static object Encode(object obj) {
            if (obj is DateTime dateTime) {
                return EncodeDateTime(dateTime);
            } else if (obj is byte[] bytes) {
                return EncodeBytes(bytes);
            } else if (obj is IList list) {
                return EncodeList(list);
            } else if (obj is IDictionary dict) {
                return EncodeDictionary(dict);
            } else if (obj is LCObject lcObj) {
                return EncodeLCObject(lcObj);
            } else if (obj is ILCOperation op) {
                return EncodeOperation(op);
            } else if (obj is ILCQueryCondition cond) {
                return EncodeQueryCondition(cond);
            } else if (obj is LCACL acl) {
                return EncodeACL(acl);
            } else if (obj is LCRelation<LCObject> relation) {
                return EncodeRelation(relation);
            } else if (obj is LCGeoPoint geoPoint) {
                return EncodeGeoPoint(geoPoint);
            }
            return obj;
        }

        public static object EncodeDateTime(DateTime dateTime) {
            DateTime utc = dateTime.ToUniversalTime();
            string str = utc.ToString(DateTimeFormat);
            return new Dictionary<string, object> {
                { "__type", "Date" },
                { "iso", str }
            };
        }

        public static object EncodeBytes(byte[] bytes) {
            string str = Convert.ToBase64String(bytes);
            return new Dictionary<string, object> {
                { "__type", "Bytes" },
                { "base64", str }
            };
        }

        public static object EncodeList(IList list) {
            List<object> l = new List<object>();
            foreach (object obj in list) {
                l.Add(Encode(obj));
            }
            return l;
        }

        public static object EncodeDictionary(IDictionary dict) {
            Dictionary<string, object> d = new Dictionary<string, object>();
            foreach (DictionaryEntry entry in dict) {
                string key = entry.Key.ToString();
                object value = entry.Value;
                d[key] = Encode(value);
            }
            return d;
        }

        public static object EncodeLCObject(LCObject obj) {
            return new Dictionary<string, object> {
                { "__type", "Pointer" },
                { "className", obj.ClassName },
                { "objectId", obj.ObjectId }
            };
        }

        static object EncodeOperation(ILCOperation operation) {
            return operation.Encode();
        }

        public static object EncodeQueryCondition(ILCQueryCondition cond) {
            return cond.Encode();
        }

        public static object EncodeACL(LCACL acl) {
            HashSet<string> keys = new HashSet<string>();
            if (acl.ReadAccess.Count > 0) {
                keys.UnionWith(acl.ReadAccess.Keys);
            }
            if (acl.WriteAccess.Count > 0) {
                keys.UnionWith(acl.WriteAccess.Keys);
            }
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in keys) {
                Dictionary<string, bool> access = new Dictionary<string, bool>();
                if (acl.ReadAccess.TryGetValue(key, out bool ra)) {
                    access["read"] = ra;
                }
                if (acl.WriteAccess.TryGetValue(key, out bool wa)) {
                    access["write"] = wa;
                }
                result[key] = access;
            }
            return result;
        }

        public static object EncodeRelation(LCRelation<LCObject> relation) {
            return new Dictionary<string, object> {
                { "__type", "Relation" },
                { "className", relation.TargetClass }
            };
        }

        public static object EncodeGeoPoint(LCGeoPoint geoPoint) {
            return new Dictionary<string, object> {
                { "__type", "GeoPoint" },
                { "latitude", geoPoint.Latitude },
                { "longitude", geoPoint.Longitude }
            };
        }
    }
}
