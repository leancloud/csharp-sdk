using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud
{
    /// <summary>
    /// Defines the class name for a subclass of AVObject.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class AVClassNameAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new AVClassName attribute.
        /// </summary>
        /// <param name="className">The class name to associate with the AVObject subclass.</param>
        public AVClassNameAttribute(string className)
        {
            this.ClassName = className;
        }

        /// <summary>
        /// Gets the class name to associate with the AVObject subclass.
        /// </summary>
        public string ClassName { get; private set; }
    }
}
