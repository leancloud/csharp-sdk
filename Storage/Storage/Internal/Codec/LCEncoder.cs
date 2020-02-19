using System;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Operation;
using LeanCloud.Storage.Internal.Query;

namespace LeanCloud.Storage.Internal.Codec {
    internal static class LCEncoder {
        internal static object Encode(object obj) {
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
            } else if (obj is LCOperation op) {
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

        static object EncodeDateTime(DateTime dateTime) {
            DateTime utc = dateTime.ToUniversalTime();
            string str = utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            return new Dictionary<string, object> {
                { "__type", "Date" },
                { "iso", str }
            };
        }

        static object EncodeBytes(byte[] bytes) {
            string str = Convert.ToBase64String(bytes);
            return new Dictionary<string, object> {
                { "__type", "Bytes" },
                { "base64", str }
            };
        }

        static object EncodeList(IList list) {
            List<object> l = new List<object>();
            foreach (object obj in list) {
                l.Add(Encode(obj));
            }
            return l;
        }

        static object EncodeDictionary(IDictionary dict) {
            Dictionary<string, object> d = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in dict) {
                string key = kv.Key;
                object value = kv.Value;
                d[key] = value;
            }
            return d;
        }

        static object EncodeLCObject(LCObject obj) {
            return new Dictionary<string, object> {
                { "__type", "Pointer" },
                { "className", obj.ClassName },
                { "objectId", obj.ObjectId }
            };
        }

        static object EncodeOperation(LCOperation operation) {
            return null;
        }

        static object EncodeQueryCondition(ILCQueryCondition cond) {
            return null;
        }

        static object EncodeACL(LCACL acl) {
            HashSet<string> readers = acl.readers;
            HashSet<string> writers = acl.writers;
            HashSet<string> union = new HashSet<string>(readers);
            union.UnionWith(writers);
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (string k in union) {
                dict[k] = new Dictionary<string, object> {
                    { "read", readers.Contains(k) },
                    { "write", writers.Contains(k) }
                };
            }
            return dict;
        }

        static object EncodeRelation(LCRelation<LCObject> relation) {
            return new Dictionary<string, object> {
                { "__type", "Relation" },
                { "className", relation.targetClass }
            };
        }

        static object EncodeGeoPoint(LCGeoPoint geoPoint) {
            return new Dictionary<string, object> {
                { "__type", "GeoPoint" },
                { "latitude", geoPoint.Latitude },
                { "longitude", geoPoint.Longitude }
            };
        }
    }
}
