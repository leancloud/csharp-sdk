using System;
namespace LeanCloud.Realtime
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class AVIMTypedMessageTypeIntAttribute : Attribute
    {
        public AVIMTypedMessageTypeIntAttribute(int typeInt)
        {
            this.TypeInteger = typeInt;
        }

        public int TypeInteger { get; private set; }
    }
}
