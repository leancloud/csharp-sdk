using System;

namespace LeanCloud.Engine {
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

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class LCEngineFunctionParameterAttribute : Attribute {
        public string ParameterName {
            get;
        }

        public LCEngineFunctionParameterAttribute(string paramName) {
            if (string.IsNullOrEmpty(paramName)) {
                throw new ArgumentNullException(nameof(paramName));
            }
            ParameterName = paramName;
        }
    }
}
