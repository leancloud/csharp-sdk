using System;
using System.Collections.Generic;
using LC.Newtonsoft.Json;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Common;

namespace LeanCloud.Realtime {
    /// <summary>
    /// Known message types.
    /// </summary>
    public class LCIMTypedMessage : LCIMMessage {
        public const int TextMessageType = -1;
        public const int ImageMessageType = -2;
        public const int AudioMessageType = -3;
        public const int VideoMessageType = -4;
        public const int LocationMessageType = -5;
        public const int FileMessageType = -6;
        public const int RecalledMessageType = -127;

        /// <summary>
        /// Preserved fields.
        /// </summary>
        protected const string MessageTypeKey = "_lctype";
        protected const string MessageAttributesKey = "_lcattrs";
        protected const string MessageTextKey = "_lctext";
        protected const string MessageLocationKey = "_lcloc";
        protected const string MessageFileKey = "_lcfile";

        protected const string MessageDataLongitudeKey = "longitude";
        protected const string MessageDataLatitudeKey = "latitude";
        
        protected const string MessageDataObjectIdKey = "objId";
        protected const string MessageDataUrlKey = "url";
        protected const string MessageDataMetaDataKey = "metaData";
        protected const string MessageDataMetaNameKey = "name";
        protected const string MessageDataMetaFormatKey = "format";
        protected const string MessageDataMetaSizeKey = "size";
        protected const string MessageDataMetaWidthKey = "width";
        protected const string MessageDataMetaHeightKey = "height";
        protected const string MessageDataMetaDurationKey = "duration";


        private Dictionary<string, object> customProperties;

        /// <summary>
        /// Complete data of message.
        /// </summary>
        protected Dictionary<string, object> data = new Dictionary<string, object>();

        public virtual int MessageType {
            get; private set;
        }

        /// <summary>
        /// Gets message attributes.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key] {
            get {
                if (customProperties == null) {
                    return null;
                }
                return customProperties[key];
            }
            set {
                if (customProperties == null) {
                    customProperties = new Dictionary<string, object>();
                }
                customProperties[key] = value;
            }
        }

        protected LCIMTypedMessage() {
        }

        internal virtual Dictionary<string, object> Encode() {
            Dictionary<string, object> msgData = data != null ?
                new Dictionary<string, object>(data) : new Dictionary<string, object>();
            msgData[MessageTypeKey] = MessageType;
            if (customProperties != null && customProperties.Count > 0) {
                msgData[MessageAttributesKey] = LCEncoder.Encode(customProperties);
            }
            return msgData;
        }

        internal virtual void Decode(Dictionary<string, object> msgData) {
            // 直接保存
            data = msgData;
        }

        internal static LCIMTypedMessage Deserialize(string json) {
            Dictionary<string, object> msgData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json,
                LCJsonConverter.Default);
            LCIMTypedMessage message = null;
            int msgType = (int)msgData[MessageTypeKey];
            if (customMessageDict.TryGetValue(msgType, out Func<LCIMTypedMessage> msgConstructor)) {
                // 已注册的类型消息
                message = msgConstructor.Invoke();
            } else {
                // 未注册的类型消息
                message = new LCIMTypedMessage();
            }
            // 已知类型消息的固定
            message.MessageType = msgType;
            if (msgData.TryGetValue(MessageAttributesKey, out object attrObj)) {
                message.customProperties = LCDecoder.Decode(attrObj) as Dictionary<string, object>;
            }
            message.Decode(msgData);
            return message;
        }

        // 内置已知类型消息
        static readonly Dictionary<int, Func<LCIMTypedMessage>> customMessageDict = new Dictionary<int, Func<LCIMTypedMessage>> {
            { TextMessageType, () => new LCIMTextMessage() },
            { ImageMessageType, () => new LCIMImageMessage() },
            { AudioMessageType, () => new LCIMAudioMessage() },
            { VideoMessageType, () => new LCIMVideoMessage() },
            { LocationMessageType, () => new LCIMLocationMessage() },
            { FileMessageType, () => new LCIMFileMessage() },
            { RecalledMessageType, () => new LCIMRecalledMessage() }
        };

        /// <summary>
        /// Registers a custom message type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msgType"></param>
        /// <param name="msgConstructor"></param>
        public static void Register<T>(int msgType, Func<T> msgConstructor)
            where T : LCIMTypedMessage {
            customMessageDict[msgType] = msgConstructor;
        }
    }
}
