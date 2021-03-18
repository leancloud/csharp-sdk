using System;

namespace LeanCloud.Engine {
    public enum LCEngineUserHookType {
        SMS,
        Email,
        OnLogin
    }

    public class LCEngineUserHookAttribute : Attribute {
        public LCEngineUserHookType HookType {
            get;
        }
 
        public LCEngineUserHookAttribute(LCEngineUserHookType hookType) {
            HookType = hookType;
        }
    }
}
