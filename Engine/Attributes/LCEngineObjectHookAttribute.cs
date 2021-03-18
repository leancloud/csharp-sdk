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

    public class LCEngineObjectHookAttribute : Attribute {
        public string ClassName {
            get;
        }

        public LCEngineObjectHookType HookType {
            get;
        }

        public LCEngineObjectHookAttribute(string className, LCEngineObjectHookType hookType) {
            ClassName = className;
            HookType = hookType;
        }
    }
}
