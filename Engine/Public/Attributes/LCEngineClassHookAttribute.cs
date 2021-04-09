using System;

namespace LeanCloud.Engine {
    public enum LCEngineObjectHookType {
        BeforeSave,
        AfterSave,
        BeforeUpdate,
        AfterUpdate,
        BeforeDelete,
        AfterDelete
    }

    public class LCEngineClassHookAttribute : Attribute {
        public string ClassName {
            get;
        }

        public LCEngineObjectHookType HookType {
            get;
        }

        public LCEngineClassHookAttribute(string className, LCEngineObjectHookType hookType) {
            if (string.IsNullOrEmpty(className)) {
                throw new ArgumentNullException(nameof(className));
            }
            ClassName = className;
            HookType = hookType;
        }
    }
}
