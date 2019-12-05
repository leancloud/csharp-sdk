using NUnit.Framework;
using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace LeanCloud.Test {
    public class LCObject {
        public string Id {
            get; set;
        }

        public Dictionary<string, object> Data {
            get; set;
        }

        public LCObject(string id) {
            Data = new Dictionary<string, object>();
            Id = id;
        }

        public static Stack<Batch> Batch(IEnumerable<LCObject> objects) {
            Stack<Batch> batches = new Stack<Batch>();
            
            IEnumerable<object> deps = objects;
            do {
                // 只添加本层依赖的 LCObject
                IEnumerable<LCObject> avObjects = deps.OfType<LCObject>();
                if (avObjects.Any()) {
                    batches.Push(new Batch(avObjects));
                }
                
                HashSet<object> childSets = new HashSet<object>();
                foreach (object dep in deps) {
                    IEnumerable children = null;
                    if (dep is IList) {
                        children = dep as IList;
                    } else if (dep is IDictionary) {
                        children = dep as IDictionary;
                    } else if (dep is LCObject) {
                        children = (dep as LCObject).Data.Values;
                    }
                    if (children != null) {
                        foreach (object child in children) {
                            childSets.Add(child);
                        }
                    }
                }
                deps = childSets;
            } while (deps != null && deps.Any());

            return batches;
        }

        public static bool HasCircleReference(object obj, HashSet<LCObject> parents) {
            if (parents.Contains(obj)) {
                return true;
            }
            IEnumerable deps = null;
            if (obj is IList) {
                deps = obj as IList;
            } else if (obj is IDictionary) {
                deps = (obj as IDictionary).Values;
            } else if (obj is LCObject) {
                deps = (obj as LCObject).Data.Values;
            }
            HashSet<LCObject> depParent = new HashSet<LCObject>(parents);
            if (obj is LCObject) {
                depParent.Add((LCObject) obj);
            }
            if (deps != null) {
                foreach (object dep in deps) {
                    HashSet<LCObject> set = new HashSet<LCObject>(depParent);
                    if (HasCircleReference(dep, set)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public Stack<Batch> Batch() {
            return Batch(new List<LCObject> { this });
        }

        public bool HasCircleReference() {
            return HasCircleReference(this, new HashSet<LCObject>());
        }
    }

    public class Batch {
        HashSet<LCObject> ObjectSet {
            get; set;
        }

        public Batch() {
            ObjectSet = new HashSet<LCObject>();
        }

        public Batch(IEnumerable<LCObject> objects) : this() {
            foreach (LCObject obj in objects) {
                ObjectSet.Add(obj);
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("----------------------------");
            foreach (LCObject obj in ObjectSet) {
                sb.AppendLine(obj.Id);
            }
            sb.AppendLine("----------------------------");
            return sb.ToString();
        }
    }

    public class AVObjectTest {
        void PrintBatches(Stack<Batch> batches) {
            while (batches.Any()) {
                Batch batch = batches.Pop();
                TestContext.WriteLine(batch);
            }
        }

        [Test]
        public void Simple() {
            LCObject a = new LCObject("a");
            LCObject b = new LCObject("b");
            LCObject c = new LCObject("c");
            a.Data["child"] = b;
            b.Data["child"] = c;

            Assert.IsFalse(a.HasCircleReference());

            Stack<Batch> batches = a.Batch();
            PrintBatches(batches);
        }

        [Test]
        public void Array() {
            LCObject a = new LCObject("a");
            LCObject b = new LCObject("b");
            LCObject c = new LCObject("c");
            a.Data["children"] = new List<LCObject> { b, c };

            Assert.IsFalse(a.HasCircleReference());

            Stack<Batch> batches = a.Batch();
            PrintBatches(batches);
        }

        [Test]
        public void SimpleCircleReference() {
            LCObject a = new LCObject("a");
            LCObject b = new LCObject("b");
            a.Data["child"] = b;
            b.Data["child"] = a;

            Assert.IsTrue(a.HasCircleReference());
        }

        [Test]
        public void ComplexCircleReference() {
            LCObject a = new LCObject("a");
            LCObject b = new LCObject("b");
            LCObject c = new LCObject("c");
            a.Data["arr"] = new List<object> { 1, b };
            a.Data["child"] = c;
            b.Data["arr"] = new List<object> { 2, a };

            Assert.IsTrue(a.HasCircleReference());
        }

        [Test]
        public void ComplexCircleReference2() {
            LCObject a = new LCObject("a");
            LCObject b = new LCObject("b");
            List<object> list = new List<object>();
            a.Data["list"] = list;
            b.Data["list"] = list;
            a.Data["child"] = b;

            Assert.IsFalse(a.HasCircleReference());
        }
    }
}
