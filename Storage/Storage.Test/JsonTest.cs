using NUnit.Framework;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LeanCloud.Storage.Internal;

namespace LeanCloudTests {
    public class JsonTest {
        [Test]
        public void Deserialize() {
            // 对象类型
            var obj = JsonConvert.DeserializeObject("{\"id\": 123}");
            TestContext.Out.WriteLine(obj.GetType());
            Assert.AreEqual(obj.GetType(), typeof(JObject));
            // 数组类型
            var arr = JsonConvert.DeserializeObject("[1, 2, 3]");
            TestContext.Out.WriteLine(arr.GetType());
            Assert.AreEqual(arr.GetType(), typeof(JArray));
            try {
                // null
                var na = JsonConvert.DeserializeObject(null);
                TestContext.Out.WriteLine(na.GetType());
            } catch (ArgumentNullException) {

            }
            Assert.Pass();
        }

        [Test]
        public void DeserializeDictionary() {
            //var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>("{\"id\": 123, \"nest\": { \"count\": 1 }}",
            //    new DictionaryConverter());
            var json = "{\"id\": 123, \"nest\": { \"count\": 1 }, \"arr\": [1, 2, 3], \"na\": null}";
            TestContext.Out.WriteLine(JsonConvert.DeserializeObject(json).GetType());
            var obj = JsonConvert.DeserializeObject<object>(json, new LeanCloudJsonConverter());
            if (obj is IDictionary<string, object>) {
                var dict = obj as Dictionary<string, object>;
                TestContext.Out.WriteLine(dict.GetType());
                TestContext.Out.WriteLine(dict["id"]);
                TestContext.Out.WriteLine(dict["nest"].GetType());
                TestContext.Out.WriteLine(dict["arr"].GetType());
            }
        }

        [Test]
        public void DeserializeList() {
            var json = "[1, \"hello\", [2, 3, 4], { \"count\": 22 }]";
            TestContext.Out.WriteLine(JsonConvert.DeserializeObject(json).GetType());
            var obj = JsonConvert.DeserializeObject<IList<object>>(json, new LeanCloudJsonConverter());
            if (obj is IList<object>) {
                var arr = obj as List<object>;
                TestContext.Out.WriteLine(arr.GetType());
                TestContext.Out.WriteLine(arr[0]);
                TestContext.Out.WriteLine(arr[1].GetType());
                TestContext.Out.WriteLine(arr[2].GetType());
                TestContext.Out.WriteLine(arr[3].GetType());
            }
        }
    }
}
