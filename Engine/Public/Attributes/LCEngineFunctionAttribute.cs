using System;

namespace LeanCloud.Engine {
    /// <summary>
    /// LCEngineFunctionAttribute is an attribute of cloud function in engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LCEngineFunctionAttribute : Attribute {
        public string FunctionName {
            get;
        }

        public LCEngineFunctionAttribute(string funcName) {
            if (string.IsNullOrEmpty(funcName)) {
                throw new ArgumentNullException(nameof(funcName));
            }
            FunctionName = funcName;
        }
    }
}
