using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;

namespace LeanCloud.Test {
    public class JustTest {
        [Test]
        public void Concat() {
            Dictionary<string, string> d1 = new Dictionary<string, string> {
                { "aaa", "111" }
            };
            Dictionary<string, string> d2 = new Dictionary<string, string> {
                { "aaa", "222" },
                { "ccc", "333" }
            };
            IEnumerable<KeyValuePair<string, string>> d = d1.Concat(d2);
            foreach (var e in d) {
                TestContext.Out.WriteLine($"{e.Key} : {e.Value}");
            }

            List<string> l1 = new List<string> { "aaa" };
            List<string> l2 = new List<string> { "aaa", "bbb" };
            IEnumerable<string> l = l1.Concat(l2);
            foreach (var e in l) {
                TestContext.Out.WriteLine($"{e}");
            }
        }

        [Test]
        public void GenericType() {
            List<int> list = new List<int> { 1, 1, 2, 3, 5, 8 };
            Type type = list.GetType();
            TestContext.Out.WriteLine(type);
            Type genericType = type.GetGenericTypeDefinition();
            TestContext.Out.WriteLine(genericType);
            TestContext.Out.WriteLine(typeof(IList<>));
            TestContext.Out.WriteLine(typeof(List<>));
        }
    }
}
