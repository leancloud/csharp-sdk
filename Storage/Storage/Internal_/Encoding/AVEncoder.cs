using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LeanCloud.Utilities;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Storage.Internal {
    /// <summary>
    /// A <c>AVEncoder</c> can be used to transform objects such as <see cref="AVObject"/> into JSON
    /// data structures.
    /// </summary>
    /// <seealso cref="AVDecoder"/>
    public abstract class AVEncoder {
        public static bool IsValidType(object value) {
            return value == null ||
                ReflectionHelpers.IsPrimitive(value.GetType()) ||
                value is string ||
                value is AVObject ||
                value is AVACL ||
                value is AVGeoPoint ||
                value is AVRelationBase ||
                value is DateTime ||
                value is byte[] ||
                Conversion.As<IDictionary<string, object>>(value) != null ||
                Conversion.As<IList<object>>(value) != null;
        }

        public object Encode(object value) {
            // If this object has a special encoding, encode it and return the
            // encoded object. Otherwise, just return the original object.
            if (value is DateTime) {
                return new Dictionary<string, object> {
                    { "__type", "Date" },
                    { "iso", ((DateTime)value).ToUniversalTime().ToString(AVClient.DateFormatStrings.First(), CultureInfo.InvariantCulture) }
                };
            }

            if (value is byte[] bytes) {
                return new Dictionary<string, object> {
                    { "__type", "Bytes" },
                    { "base64", Convert.ToBase64String(bytes) }
                };
            }

            if (value is AVObject obj) {
                return EncodeAVObject(obj);
            }

            if (value is IJsonConvertible jsonConvertible) {
                return jsonConvertible.ToJSON();
            }

            if (value is IDictionary) {
                IDictionary dict = value as IDictionary;
                var json = new Dictionary<string, object>();
                foreach (var key in dict.Keys) {
                    object v = dict[key];
                    json[key.ToString()] = Encode(v);
                }
                return json;
            }

            if (value is IList) {
                IList list = value as IList;
                return EncodeList(list);
            }

            if (value is IAVFieldOperation operation) {
                return operation.Encode();
            }

            return value;
        }

        protected abstract IDictionary<string, object> EncodeAVObject(AVObject value);

        private object EncodeList(IList list) {
            List<object> newArray = new List<object>();
            foreach (object item in list) {
                newArray.Add(Encode(item));
            }
            return newArray;
        }
    }
}
