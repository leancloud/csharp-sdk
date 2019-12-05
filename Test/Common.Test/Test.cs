using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Common.Test {
    public class Test {
        [Test]
        public async Task AsyncFor() {
            for (int i = 0; i < 5; i++) {
                await Task.Delay(1000);
                TestContext.WriteLine($"{i} done at {DateTimeOffset.UtcNow}");
            }
        }

        [Test]
        public void ConcurrentCollection() {
            List<int> list = new List<int>();
            for (int i = 0; i < 1000; i++) {
                Task.Run(() => {
                    list.Add(i);
                });
            }
            TestContext.WriteLine($"{list.Count}");

            ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
            for (int i = 0; i < 1000; i++) {
                Task.Run(() => {
                    queue.Enqueue(i);
                });
            }
            TestContext.WriteLine($"{queue.Count}");
        }

        [Test]
        public void ObjectType() {
            List<object> list = new List<object> { 1, "hello", 2, "world" };
            TestContext.WriteLine(list is IList);
            object[] objs = { 1, "hi", 3 };
            TestContext.WriteLine(objs is IList);
            List<object> subList = list.OfType<string>().ToList<object>();
            foreach (object obj in subList) {
                TestContext.WriteLine(obj);
            }
        }

        [Test]
        public void CollectionExcept() {
            List<int> list1 = new List<int> { 1, 2, 3, 4, 5 };
            List<int> list2 = new List<int> { 4, 5, 6 };
            IEnumerable<int> deltaList = list1.Except(list2).ToList();
            foreach (int delta in deltaList) {
                TestContext.WriteLine(delta);
            }

            Dictionary<string, object> dict1 = new Dictionary<string, object> {
                { "a", 1 },
                { "b", 2 }
            };
            Dictionary<string, object> dict2 = new Dictionary<string, object> {
                { "b", 2 },
                { "c", 3 }
            };
            IEnumerable<KeyValuePair<string, object>> deltaDict = dict1.Except(dict2);
            foreach (KeyValuePair<string, object> delta in deltaDict) {
                TestContext.WriteLine($"{delta.Key} : {delta.Value}");
            }
        }
    }
}
