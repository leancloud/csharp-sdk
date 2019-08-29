using System;

namespace LeanCloud {
    /// <summary>
    /// Specifies a field name for a property on a AVObject subclass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class AVFieldNameAttribute : Attribute {
        /// <summary>
        /// Constructs a new AVFieldName attribute.
        /// </summary>
        /// <param name="fieldName">The name of the field on the AVObject that the
        /// property represents.</param>
        public AVFieldNameAttribute(string fieldName) {
            FieldName = fieldName;
        }

        /// <summary>
        /// Gets the name of the field represented by this property.
        /// </summary>
        public string FieldName { get; private set; }
    }
}
