using System;
using System.Collections.Generic;
using LeanCloud.Play.Protocol;
using LC.Google.Protobuf;

namespace LeanCloud.Play {
    /// <summary>
    /// 序列化方法委托
    /// </summary>
    public delegate byte[] SerializeMethod(object obj);
    /// <summary>
    /// 反序列化方法委托
    /// </summary>
    public delegate object DeserializeMethod(byte[] bytes);

    /// <summary>
    /// 序列化工具类
    /// </summary>
    public static class CodecUtils {
        static readonly Dictionary<Type, CustomType> typeDict = new Dictionary<Type, CustomType>();
        static readonly Dictionary<int, CustomType> typeIdDict = new Dictionary<int, CustomType>();

        /// <summary>
        /// 注册自定义类型的序列化
        /// </summary>
        /// <returns><c>true</c>, if type was registered, <c>false</c> otherwise.</returns>
        /// <param name="type">类型</param>
        /// <param name="typeId">类型 Id</param>
        /// <param name="serializeMethod">序列化方法</param>
        /// <param name="deserializeMethod">反序列化方法</param>
        public static bool RegisterType(Type type, int typeId, SerializeMethod serializeMethod, DeserializeMethod deserializeMethod) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (serializeMethod == null) {
                throw new ArgumentNullException(nameof(serializeMethod));
            }
            if (deserializeMethod == null) {
                throw new ArgumentNullException(nameof(deserializeMethod));
            }
            if (typeDict.ContainsKey(type) || typeIdDict.ContainsKey(typeId)) {
                return false;
            }
            var customType = new CustomType(type, typeId, serializeMethod, deserializeMethod);
            typeDict.Add(type, customType);
            typeIdDict.Add(typeId, customType);
            return true;
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <returns>The encode.</returns>
        /// <param name="val">要序列化的对象</param>
        public static GenericCollectionValue Serialize(object val) {
            GenericCollectionValue genericVal = null;
            if (val is null) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Null
                };
            } else if (val is byte[]) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Bytes,
                    BytesValue = ByteString.CopyFrom((byte[])val)
                };
            } else if (val is byte) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Byte,
                    IntValue = (byte)val
                };
            } else if (val is short) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Short,
                    IntValue = (short)val
                };
            } else if (val is int) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Int,
                    IntValue = (int)val
                };
            } else if (val is long) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Long,
                    LongIntValue = (long)val
                };
            } else if (val is bool) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Bool,
                    BoolValue = (bool)val
                };
            } else if (val is float) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Float,
                    FloatValue = (float)val
                };
            } else if (val is double) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Double,
                    DoubleValue = (double)val
                };
            } else if (val is string) {
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.String,
                    StringValue = (string)val
                };
            } else if (val is PlayObject playObject) {
                var bytes = SerializePlayObject(playObject);
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Map,
                    BytesValue = ByteString.CopyFrom(bytes)
                };
            } else if (val is PlayArray playArray) {
                var collection = new GenericCollection();
                foreach (object obj in playArray) {
                    collection.ListValue.Add(Serialize(obj));
                }
                genericVal = new GenericCollectionValue {
                    Type = GenericCollectionValue.Types.Type.Array,
                    BytesValue = collection.ToByteString()
                };
            } else {
                var type = val.GetType();
                if (typeDict.TryGetValue(type, out var customType)) {
                    genericVal = new GenericCollectionValue {
                        Type = GenericCollectionValue.Types.Type.Object,
                        ObjectTypeId = customType.TypeId,
                        BytesValue = ByteString.CopyFrom(customType.SerializeMethod(val))
                    };
                } else {
                    throw new Exception($"{type} is not supported");
                }
            }

            return genericVal;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <returns>The decode.</returns>
        /// <param name="genericValue">带类型的序列化对象</param>
        public static object Deserialize(GenericCollectionValue genericValue) {
            object val = null;
            switch (genericValue.Type) {
                case GenericCollectionValue.Types.Type.Null:
                    // val = null;
                    break;
                case GenericCollectionValue.Types.Type.Bytes:
                    val = genericValue.BytesValue.ToByteArray();
                    break;
                case GenericCollectionValue.Types.Type.Byte:
                    val = (byte)genericValue.IntValue;
                    break;
                case GenericCollectionValue.Types.Type.Short:
                    val = (short)genericValue.IntValue;
                    break;
                case GenericCollectionValue.Types.Type.Int:
                    val = genericValue.IntValue;
                    break;
                case GenericCollectionValue.Types.Type.Long:
                    val = genericValue.LongIntValue;
                    break;
                case GenericCollectionValue.Types.Type.Bool:
                    val = genericValue.BoolValue;
                    break;
                case GenericCollectionValue.Types.Type.Float:
                    val = genericValue.FloatValue;
                    break;
                case GenericCollectionValue.Types.Type.Double:
                    val = genericValue.DoubleValue;
                    break;
                case GenericCollectionValue.Types.Type.String:
                    val = genericValue.StringValue;
                    break;
                case GenericCollectionValue.Types.Type.Map:
                    val = DeserializePlayObject(genericValue.BytesValue);
                    break;
                case GenericCollectionValue.Types.Type.Array: {
                        PlayArray playArray = new PlayArray();
                        var collection = GenericCollection.Parser.ParseFrom(genericValue.BytesValue);
                        foreach (var element in collection.ListValue) {
                            playArray.Add(Deserialize(element));
                        }
                        val = playArray;
                    }
                    break;
                case GenericCollectionValue.Types.Type.Object: {
                        // 自定义类型
                        var typeId = genericValue.ObjectTypeId;
                        if (typeIdDict.TryGetValue(typeId, out var customType)) {
                            val = customType.DeserializeMethod(genericValue.BytesValue.ToByteArray());
                        } else {
                            throw new Exception($"type id: {typeId} is not supported");
                        }
                    }
                    break;
                default:
                    // 异常
                    throw new Exception($"{genericValue.Type} is not supported");
            }
            return val;
        }

        /// <summary>
        /// 序列化 PlayObject 对象
        /// </summary>
        /// <returns>The play object.</returns>
        /// <param name="playObject">PlayObject 对象</param>
        public static byte[] SerializePlayObject(PlayObject playObject) {
            if (playObject == null) {
                return null;
            }
            var collection = new GenericCollection();
            foreach (var entry in playObject) {
                collection.MapEntryValue.Add(new GenericCollection.Types.MapEntry {
                    Key = entry.Key as string,
                    Val = Serialize(entry.Value)
                });
            }
            return collection.ToByteArray();
        }

        /// <summary>
        /// 反序列化 PlayObject 对象
        /// </summary>
        /// <returns>PlayObject 对象</returns>
        /// <param name="bytes">要反序列化的字节码</param>
        public static PlayObject DeserializePlayObject(byte[] bytes) {
            var collection = GenericCollection.Parser.ParseFrom(bytes);
            var playObject = new PlayObject();
            foreach (var entry in collection.MapEntryValue) {
                playObject[entry.Key] = Deserialize(entry.Val);
            }
            return playObject; 
        }

        /// <summary>
        /// 反序列化 PlayObject 对象
        /// </summary>
        /// <param name="byteString">要反序列化的字符串</param>
        /// <returns>PlayObject 对象</returns>
        public static PlayObject DeserializePlayObject(ByteString byteString) { 
            if (byteString == null) {
                return null;
            }
            return DeserializePlayObject(byteString.ToByteArray());
        }
    }
}
