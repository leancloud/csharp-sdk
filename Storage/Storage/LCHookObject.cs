using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LeanCloud.Storage.Internal.Operation;

namespace LeanCloud.Storage {
    public static class LCClassHook {
        public const string BeforeSave = "beforeSave";
        public const string AfterSave = "afterSave";
        public const string BeforeUpdate = "beforeUpdate";
        public const string AfterUpdate = "afterUpdate";
        public const string BeforeDelete = "beforeDelete";
        public const string AfterDelete = "afterDelete";
    }

    public partial class LCObject {
        internal const string IgnoreHooksKey = "__ignore_hooks";

        public void DisableBeforeHook() {
            Ignore(
                LCClassHook.BeforeSave,
                LCClassHook.BeforeUpdate,
                LCClassHook.BeforeDelete);
        }

        public void DisableAfterHook() {
            Ignore(
                LCClassHook.AfterSave,
                LCClassHook.AfterUpdate,
                LCClassHook.AfterDelete);
        }

        public void IgnoreHook(string hookName) {
            if (hookName != LCClassHook.BeforeSave && hookName != LCClassHook.AfterSave &&
                hookName != LCClassHook.BeforeUpdate && hookName != LCClassHook.AfterUpdate &&
                hookName != LCClassHook.BeforeDelete && hookName != LCClassHook.AfterDelete) {
                throw new ArgumentException($"Invalid {hookName}");
            }

            Ignore(hookName);
        }

        private void Ignore(params string[] hooks) {
            LCIgnoreHookOperation op = new LCIgnoreHookOperation(hooks);
            ApplyOperation(IgnoreHooksKey, op);
        }

        public ReadOnlyCollection<string> UpdatedKeys {
            get {
                return (this["_updatedKeys"] as List<object>)
                    .Cast<string>()
                    .ToList()
                    .AsReadOnly();
            }
        }
    }
}
