using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LeanCloud.Storage.Internal {
    public class LeanCloudJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(object);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.StartObject) {
                var obj = new Dictionary<string, object>();
                serializer.Populate(reader, obj);
                return obj;
            }
            if (reader.TokenType == JsonToken.StartArray) {
                var arr = new List<object>();
                serializer.Populate(reader, arr);
                return arr;
            }

            return serializer.Deserialize(reader);
        }
    }
}
