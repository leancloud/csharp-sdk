using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeanCloud.Realtime.Internal
{
    interface IFreeStyleMessageClassingController
    {
        bool IsTypeValid(IDictionary<string,object> msg, Type type);
        void RegisterSubclass(Type t);
        IAVIMMessage Instantiate(string msgStr,IDictionary<string,object> buildInData);
        IDictionary<string, object> EncodeProperties(IAVIMMessage subclass);
        Type GetType(IDictionary<string, object> msg);
        String GetClassName(Type type);
        IDictionary<String, String> GetPropertyMappings(String className);
    }
}
