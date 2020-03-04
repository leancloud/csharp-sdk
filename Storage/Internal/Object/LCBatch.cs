using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal.Object {
    internal class LCBatch {
        internal HashSet<LCObject> objects;

        internal LCBatch(IEnumerable<LCObject> objs) {
            if (objs == null) {
                objects = new HashSet<LCObject>();
            } else {
                objects = new HashSet<LCObject>(objs);
            }
        }

        internal static bool HasCircleReference(object obj, HashSet<LCObject> parents) {
            if (obj is LCObject lcObj && parents.Contains(lcObj)) {
                return true;
            }
            IEnumerable deps = null;
            if (obj is IList list) {
                deps = list;
            } else if (obj is IDictionary dict) {
                deps = dict.Values;
            } else if (obj is LCObject lcObject) {
                deps = lcObject.estimatedData.Values;
            }
            HashSet<LCObject> depParents = new HashSet<LCObject>(parents);
            if (obj is LCObject) {
                depParents.Add(obj as LCObject);
            }
            if (deps != null) {
                foreach (object dep in deps) {
                    HashSet<LCObject> ps = new HashSet<LCObject>(depParents);
                    if (HasCircleReference(dep, ps)) {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static Stack<LCBatch> BatchObjects(IEnumerable<LCObject> objects, bool containSelf) {
            Stack<LCBatch> batches = new Stack<LCBatch>();
            if (containSelf) {
                batches.Push(new LCBatch(objects));
            }
            HashSet<object> deps = new HashSet<object>();
            foreach (LCObject obj in objects) {
                deps.UnionWith(obj.operationDict.Values.Select(op => op.GetNewObjectList()));
            }
            do {
                HashSet<object> childSet = new HashSet<object>();
                foreach (object dep in deps) {
                    IEnumerable children = null;
                    if (dep is IList list) {
                        children = list;
                    } else if (dep is IDictionary dict) {
                        children = dict;
                    } else if (dep is LCObject lcDep && lcDep.ObjectId == null) {
                        children = lcDep.operationDict.Values.Select(op => op.GetNewObjectList());
                    }
                    if (children != null) {
                        childSet.UnionWith(children.Cast<object>());
                    }
                }
                IEnumerable<LCObject> depObjs = deps.Where(item => item is LCObject lcItem && lcItem.ObjectId == null)
                                                    .Cast<LCObject>();
                if (depObjs != null && depObjs.Count() > 0) {
                    batches.Push(new LCBatch(depObjs));
                }
                deps = childSet;
            } while (deps != null && deps.Count > 0);
            return batches;
        }
    }
}
