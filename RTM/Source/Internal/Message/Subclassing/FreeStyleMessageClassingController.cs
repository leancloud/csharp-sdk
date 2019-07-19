using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace LeanCloud.Realtime.Internal
{
    internal class FreeStyleMessageClassingController : IFreeStyleMessageClassingController
    {
        private static readonly string messageClassName = "_AVIMMessage";
        private readonly IDictionary<string, FreeStyleMessageClassInfo> registeredInterfaces;
        private readonly ReaderWriterLockSlim mutex;

        public FreeStyleMessageClassingController()
        {
            mutex = new ReaderWriterLockSlim();
            registeredInterfaces = new Dictionary<string, FreeStyleMessageClassInfo>();
        }

        public Type GetType(IDictionary<string, object> msg)
        {
            throw new NotImplementedException();
        }

        public IAVIMMessage Instantiate(string msgStr, IDictionary<string, object> buildInData)
        {
            FreeStyleMessageClassInfo info = null;
            mutex.EnterReadLock();
            bool bin = false;
            if (buildInData.ContainsKey("bin"))
            {
                bool.TryParse(buildInData["bin"].ToString(), out bin);
            }

            if (bin)
            {
                var binMessage = new AVIMBinaryMessage();
                this.DecodeProperties(binMessage, buildInData);
                return binMessage;
            }

            var reverse = registeredInterfaces.Values.Reverse();
            foreach (var subInterface in reverse)
            {
                if (subInterface.Validate(msgStr))
                {
                    info = subInterface;
                    break;
                }
            }

            mutex.ExitReadLock();

            var message = info != null ? info.Instantiate(msgStr) : new AVIMMessage();

            this.DecodeProperties(message, buildInData);

            message.Deserialize(msgStr);

            return message;
        }

        public IAVIMMessage DecodeProperties(IAVIMMessage message, IDictionary<string, object> buildInData)
        {
            long timestamp;
            if (buildInData.ContainsKey("timestamp"))
            {
                if (long.TryParse(buildInData["timestamp"].ToString(), out timestamp))
                {
                    message.ServerTimestamp = timestamp;
                }
            }
            long ackAt;
            if (buildInData.ContainsKey("ackAt"))
            {
                if (long.TryParse(buildInData["ackAt"].ToString(), out ackAt))
                {
                    message.RcpTimestamp = ackAt;
                }
            }

            if (buildInData.ContainsKey("from"))
            {
                message.FromClientId = buildInData["from"].ToString();
            }
            if (buildInData.ContainsKey("msgId"))
            {
                message.Id = buildInData["msgId"].ToString();
            }
            if (buildInData.ContainsKey("cid"))
            {
                message.ConversationId = buildInData["cid"].ToString();
            }
            if (buildInData.ContainsKey("fromPeerId"))
            {
                message.FromClientId = buildInData["fromPeerId"].ToString();
            }
            if (buildInData.ContainsKey("id"))
            {
                message.Id = buildInData["id"].ToString();
            }
            if (buildInData.ContainsKey("mid"))
            {
                message.Id = buildInData["mid"].ToString();
            }
            if (buildInData.ContainsKey("mentionPids"))
            {
                message.MentionList = AVDecoder.Instance.DecodeList<string>(buildInData["mentionPids"]);
            }
            if (buildInData.TryGetValue("patchTimestamp", out object patchTimestampObj)) {
                if (long.TryParse(patchTimestampObj.ToString(), out long patchTimestamp)) {
                    message.UpdatedAt = patchTimestamp;
                }
            }

            bool mentionAll;
            if (buildInData.ContainsKey("mentionAll"))
            {
                if (bool.TryParse(buildInData["mentionAll"].ToString(), out mentionAll))
                {
                    message.MentionAll = mentionAll;
                }
            }
            return message;
        }

        public IDictionary<string, object> EncodeProperties(IAVIMMessage subclass)
        {
            var type = subclass.GetType();
            var result = new Dictionary<string, object>();
            var className = GetClassName(type);
            var typeInt = GetTypeInt(type);
            var propertMappings = GetPropertyMappings(className);
            foreach (var propertyPair in propertMappings)
            {
                var propertyInfo = ReflectionHelpers.GetProperty(type, propertyPair.Key);
                var operation = propertyInfo.GetValue(subclass, null);
                if (operation != null)
                    result[propertyPair.Value] = PointerOrLocalIdEncoder.Instance.Encode(operation);
            }
            if (typeInt != 0)
            {
                result[AVIMProtocol.LCTYPE] = typeInt;
            }
            return result;
        }

        public bool IsTypeValid(IDictionary<string, object> msg, Type type)
        {
            return true;
        }

        public void RegisterSubclass(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();

            if (!typeof(IAVIMMessage).GetTypeInfo().IsAssignableFrom(typeInfo))
            {
                throw new ArgumentException("Cannot register a type that is not a implementation of IAVIMMessage");
            }
            var className = GetClassName(type);
            var typeInt = GetTypeInt(type);
            try
            {
                mutex.EnterWriteLock();
                ConstructorInfo constructor = type.FindConstructor();
                if (constructor == null)
                {
                    throw new ArgumentException("Cannot register a type that does not implement the default constructor!");
                }
                var classInfo = new FreeStyleMessageClassInfo(type, constructor);
                if (typeInt != 0)
                {
                    classInfo.TypeInt = typeInt;
                }
                registeredInterfaces[className] = classInfo;
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }
        public String GetClassName(Type type)
        {
            return type == typeof(IAVIMMessage)
              ? messageClassName
              : FreeStyleMessageClassInfo.GetMessageClassName(type.GetTypeInfo());
        }
        public int GetTypeInt(Type type)
        {
            return type == typeof(AVIMTypedMessage) ? 0 : FreeStyleMessageClassInfo.GetTypedInteger(type.GetTypeInfo());
        }
        public IDictionary<String, String> GetPropertyMappings(String className)
        {
            FreeStyleMessageClassInfo info = null;
            mutex.EnterReadLock();
            registeredInterfaces.TryGetValue(className, out info);
            if (info == null)
            {
                registeredInterfaces.TryGetValue(messageClassName, out info);
            }
            mutex.ExitReadLock();

            return info.PropertyMappings;
        }
    }
}
