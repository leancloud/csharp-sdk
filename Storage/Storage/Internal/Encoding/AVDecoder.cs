using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using LeanCloud.Utilities;

namespace LeanCloud.Storage.Internal
{
    public class AVDecoder
    {
        // This class isn't really a Singleton, but since it has no state, it's more efficient to get
        // the default instance.
        private static readonly AVDecoder instance = new AVDecoder();
        public static AVDecoder Instance
        {
            get
            {
                return instance;
            }
        }

        // Prevent default constructor.
        private AVDecoder() { }

        public object Decode(object data)
        {
            if (data == null)
            {
                return null;
            }

            var dict = data as IDictionary<string, object>;
            if (dict != null)
            {
                if (dict.ContainsKey("__op"))
                {
                    return AVFieldOperations.Decode(dict);
                }

                object type;
                dict.TryGetValue("__type", out type);
                var typeString = type as string;

                if (typeString == null)
                {
                    var newDict = new Dictionary<string, object>();
                    foreach (var pair in dict)
                    {
                        newDict[pair.Key] = Decode(pair.Value);
                    }
                    return newDict;
                }

                if (typeString == "Date")
                {
                    return ParseDate(dict["iso"] as string);
                }

                if (typeString == "Bytes")
                {
                    return Convert.FromBase64String(dict["base64"] as string);
                }

                if (typeString == "Pointer")
                {
                    //set a include key to fetch or query.
                    if (dict.Keys.Count > 3)
                    {
                        return DecodeAVObject(dict);
                    }
                    return DecodePointer(dict["className"] as string, dict["objectId"] as string);
                }

                if (typeString == "File")
                {
                    return DecodeAVFile(dict);
                }

                if (typeString == "GeoPoint")
                {
                    return new AVGeoPoint(Conversion.To<double>(dict["latitude"]),
                        Conversion.To<double>(dict["longitude"]));
                }

                if (typeString == "Object")
                {
                    return DecodeAVObject(dict);
                }

                if (typeString == "Relation")
                {
                    return AVRelationBase.CreateRelation(null, null, dict["className"] as string);
                }

                var converted = new Dictionary<string, object>();
                foreach (var pair in dict)
                {
                    converted[pair.Key] = Decode(pair.Value);
                }
                return converted;
            }

            var list = data as IList<object>;
            if (list != null)
            {
                return (from item in list
                        select Decode(item)).ToList();
            }

            return data;
        }

        protected virtual object DecodePointer(string className, string objectId)
        {
            if (className == "_File")
            {
                return AVFile.CreateWithoutData(objectId);
            }
            return AVObject.CreateWithoutData(className, objectId);
        }
        protected virtual object DecodeAVObject(IDictionary<string, object> dict)
        {
            var className = dict["className"] as string;
            if (className == "_File")
            {
                return DecodeAVFile(dict);
            }
            var state = AVObjectCoder.Instance.Decode(dict, this);
            return AVObject.FromState<AVObject>(state, dict["className"] as string);
        }
        protected virtual object DecodeAVFile(IDictionary<string, object> dict)
        {
            var objectId = dict["objectId"] as string;
            var file = AVFile.CreateWithoutData(objectId);
            file.MergeFromJSON(dict);
            return file;
        }


        public virtual IList<T> DecodeList<T>(object data)
        {
            IList<T> rtn = null;
            var list = (IList<object>)data;
            if (list != null)
            {
                rtn = new List<T>();
                foreach (var item in list)
                {
                    rtn.Add((T)item);
                }
            }
            return rtn;
        }

        public static DateTime ParseDate(string input)
        {
            var rtn = DateTime.ParseExact(input,
              AVClient.DateFormatStrings,
              CultureInfo.InvariantCulture,
              DateTimeStyles.AssumeUniversal);
            return rtn;
        }
    }
}
