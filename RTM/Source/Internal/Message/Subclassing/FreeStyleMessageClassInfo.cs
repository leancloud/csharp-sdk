using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LeanCloud.Realtime.Internal
{
    internal class FreeStyleMessageClassInfo
    {
        public TypeInfo TypeInfo { get; private set; }
        public IDictionary<String, String> PropertyMappings { get; private set; }
        private ConstructorInfo Constructor { get; set; }
        //private MethodInfo ValidateMethod { get; set; }

        public int TypeInt { get; set; }

        public FreeStyleMessageClassInfo(Type type, ConstructorInfo constructor)
        {
            TypeInfo = type.GetTypeInfo();
            Constructor = constructor;
            PropertyMappings = ReflectionHelpers.GetProperties(type)
              .Select(prop => Tuple.Create(prop, prop.GetCustomAttribute<AVIMMessageFieldNameAttribute>(true)))
              .Where(t => t.Item2 != null)
              .Select(t => Tuple.Create(t.Item1, t.Item2.FieldName))
              .ToDictionary(t => t.Item1.Name, t => t.Item2);
        }
        public bool Validate(string msgStr)
        {
            var instance = Instantiate(msgStr);
            if (instance is AVIMTypedMessage)
            {
                try
                {
                    var msgDic = Json.Parse(msgStr) as IDictionary<string, object>;
                    if (msgDic != null)
                    {
                        if (msgDic.ContainsKey(AVIMProtocol.LCTYPE))
                        {
                            return msgDic[AVIMProtocol.LCTYPE].ToString() == TypeInt.ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException)
                    {
                        return instance.Validate(msgStr);
                    }
                }

            }
            return instance.Validate(msgStr);
        }

        public IAVIMMessage Instantiate(string msgStr)
        {
            var rtn = (IAVIMMessage)Constructor.Invoke(null);
            return rtn;
        }

        public static string GetMessageClassName(TypeInfo type)
        {
            var attribute = type.GetCustomAttribute<AVIMMessageClassNameAttribute>();
            return attribute != null ? attribute.ClassName : null;
        }

        public static int GetTypedInteger(TypeInfo type)
        {
            var attribute = type.GetCustomAttribute<AVIMTypedMessageTypeIntAttribute>();
            return attribute != null ? attribute.TypeInteger : 0;
        }
    }
}
