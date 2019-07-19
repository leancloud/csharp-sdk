using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class AVIMMessageClassNameAttribute: Attribute
    {
        public AVIMMessageClassNameAttribute(string className)
        {
            this.ClassName = className;
        }
        public string ClassName { get; private set; }

    }
}
