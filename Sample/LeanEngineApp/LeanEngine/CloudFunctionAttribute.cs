using System;

namespace LeanEngineApp.LeanEngine {
    [AttributeUsage(AttributeTargets.Method)]
    public class CloudFunctionAttribute : Attribute {
        public string Name {
            get;
        }

        public CloudFunctionAttribute(string name) {
            Name = name;
        }
    }
}
