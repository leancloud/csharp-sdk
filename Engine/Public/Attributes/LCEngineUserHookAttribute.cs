using System;

namespace LeanCloud.Engine {
    public enum LCEngineUserHookType {
        OnSMSVerified,
        OnEmailVerified,
        OnLogin
    }

    /// <summary>
    /// LCEngineUserHookAttribute is an attribute that hooks user in engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LCEngineUserHookAttribute : Attribute {
        public LCEngineUserHookType HookType {
            get;
        }
 
        public LCEngineUserHookAttribute(LCEngineUserHookType hookType) {
            HookType = hookType;
        }
    }
}
