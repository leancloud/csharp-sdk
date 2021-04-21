using System;

namespace LeanCloud.Engine {
    /// <summary>
    /// LCEngineFunctionParamAttribute is an attribute of the parameter of cloud function in engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class LCEngineFunctionParamAttribute : Attribute {
        public string ParamName {
            get;
        }

        public LCEngineFunctionParamAttribute(string paramName) {
            if (string.IsNullOrEmpty(paramName)) {
                throw new ArgumentNullException(nameof(paramName));
            }
            ParamName = paramName;
        }
    }
}
