using System;

namespace LeanCloud.Engine {
    public enum LCEngineUserHookType {
        OnSMSVerified,
        OnEmailVerified,
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
