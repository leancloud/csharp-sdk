using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LeanCloud.Storage.Internal.Operation {
    internal class LCIgnoreHookOperation : ILCOperation {
        internal HashSet<string> ignoreHooks;

        internal LCIgnoreHookOperation(IEnumerable<string> hooks) {
            ignoreHooks = new HashSet<string>(hooks);
        }

        public object Apply(object oldValue, string key) {
            HashSet<object> set = new HashSet<object>();
            if (oldValue != null) {
                set.UnionWith(oldValue as IEnumerable<object>);
            }
            set.UnionWith(ignoreHooks);
            return set.ToList();
        }

        public object Encode() {
            return ignoreHooks;
        }

        public IEnumerable GetNewObjectList() {
            return ignoreHooks;
        }

        public ILCOperation MergeWithPrevious(ILCOperation previousOp) {
            if (previousOp is LCIgnoreHookOperation ignoreHookOp) {
                ignoreHooks.UnionWith(ignoreHookOp.ignoreHooks);
                return this;
            }
            throw new ArgumentException("Operation is invalid after previous operation.");
        }
    }
}
