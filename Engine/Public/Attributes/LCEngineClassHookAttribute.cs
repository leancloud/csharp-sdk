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

    /// <summary>
    /// LCEngineClassHookAttribute is an attribute that hooks class in LeanEngine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
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
