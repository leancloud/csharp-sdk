using System;

namespace LeanCloud.Play {
    internal class CustomType {
        internal Type Type {
            get;
        }

        internal int TypeId {
            get;
        }

        internal SerializeMethod SerializeMethod {
            get;
        }

        internal DeserializeMethod DeserializeMethod {
            get;
        }

        internal CustomType(Type type, int typeId, SerializeMethod encodeFunc, DeserializeMethod decodeFunc) {
            Type = type;
            TypeId = typeId;
            SerializeMethod = encodeFunc;
            DeserializeMethod = decodeFunc;
        }
    }
}
