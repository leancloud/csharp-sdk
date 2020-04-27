using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 已知类型消息
    /// </summary>
    public class LCIMTypedMessage : LCIMMessage {
        /// <summary>
        /// 文本消息
        /// </summary>
        public const int TextMessageType = -1;
        /// <summary>
        /// 图像消息
        /// </summary>
        public const int ImageMessageType = -2;
        /// <summary>
        /// 音频消息
        /// </summary>
        public const int AudioMessageType = -3;
        /// <summary>
        /// 视频消息
        /// </summary>
        public const int VideoMessageType = -4;
        /// <summary>
        /// 位置消息
        /// </summary>
        public const int LocationMessageType = -5;
        /// <summary>
        /// 文件消息
        /// </summary>
        public const int FileMessageType = -6;
        /// <summary>
        /// 撤回消息
        /// </summary>
        public const int RecalledMessageType = -127;

        /// <summary>
        /// 保留字段
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
        /// 完整的消息数据
        /// </summary>
        protected Dictionary<string, object> data = new Dictionary<string, object>();

        /// <summary>
        /// 消息类型
        /// </summary>
        public virtual int MessageType {
            get; private set;
        }

        /// <summary>
        /// 消息属性访问
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
            data = msgData;
            MessageType = (int)msgData[MessageTypeKey];
            if (msgData.TryGetValue(MessageAttributesKey, out object attrObj)) {
                customProperties = LCDecoder.Decode(attrObj) as Dictionary<string, object>;
            }
        }

        internal static LCIMTypedMessage Deserialize(string json) {
            Dictionary<string, object> msgData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json,
                new LCJsonConverter());
            LCIMTypedMessage message = null;
            int msgType = (int)msgData[MessageTypeKey];
            if (customMessageDict.TryGetValue(msgType, out Func<LCIMTypedMessage> msgConstructor)) {
                // 已注册的类型消息
                message = msgConstructor.Invoke();
            } else {
                // 未注册的类型消息
                message = new LCIMTypedMessage();
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
        /// 注册自定义类型消息
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
