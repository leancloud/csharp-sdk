using LeanCloud.Storage.Internal;
using LeanCloud.Realtime.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 
    /// </summary>
    [AVIMMessageClassName("_AVIMTypedMessage")]
    [AVIMTypedMessageTypeInt(0)]
    public class AVIMTypedMessage : AVIMMessage, IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:LeanCloud.Realtime.AVIMTypedMessage"/> class.
        /// </summary>
        public AVIMTypedMessage()
        {

        }

        /// <summary>
        /// 文本内容
        /// </summary>
        [AVIMMessageFieldName("_lctext")]
        public string TextContent
        {
            get; set;
        }

        private IDictionary<string, object> estimatedData = new Dictionary<string, object>();
        /// <summary>
        /// Serialize this instance.
        /// </summary>
        /// <returns>The serialize.</returns>
        public override string Serialize()
        {
            var result = Encode();
            var resultStr = Json.Encode(result);
            this.Content = resultStr;
            return resultStr;
        }

        /// <summary>
        /// Encode this instance.
        /// </summary>
        /// <returns>The encode.</returns>
        public virtual IDictionary<string, object> Encode()
        {
            var result = AVRealtime.FreeStyleMessageClassingController.EncodeProperties(this);
            var encodedAttrs = PointerOrLocalIdEncoder.Instance.Encode(estimatedData);
            result[AVIMProtocol.LCATTRS] = estimatedData;
            return result;
        }

        /// <summary>
        /// Validate the specified msgStr.
        /// </summary>
        /// <returns>The validate.</returns>
        /// <param name="msgStr">Message string.</param>
        public override bool Validate(string msgStr)
        {
            try
            {
                var msg = Json.Parse(msgStr) as IDictionary<string, object>;
                return msg.ContainsKey(AVIMProtocol.LCTYPE);
            }
            catch
            {

            }
            return false;
        }

        /// <summary>
        /// Deserialize the specified msgStr.
        /// </summary>
        /// <returns>The deserialize.</returns>
        /// <param name="msgStr">Message string.</param>
        public override IAVIMMessage Deserialize(string msgStr)
        {
            var msg = Json.Parse(msgStr) as IDictionary<string, object>;
            var className = AVRealtime.FreeStyleMessageClassingController.GetClassName(this.GetType());
            var PropertyMappings = AVRealtime.FreeStyleMessageClassingController.GetPropertyMappings(className);
            var messageFieldProperties = PropertyMappings.Where(prop => msg.ContainsKey(prop.Value))
                  .Select(prop => Tuple.Create(ReflectionHelpers.GetProperty(this.GetType(), prop.Key), msg[prop.Value]));

            foreach (var property in messageFieldProperties)
            {
                property.Item1.SetValue(this, property.Item2, null);
            }

            if (msg.ContainsKey(AVIMProtocol.LCATTRS))
            {
                object attrs = msg[AVIMProtocol.LCATTRS];
                this.estimatedData = AVDecoder.Instance.Decode(attrs) as Dictionary<string, object>;
            }

            return base.Deserialize(msgStr);
        }

        /// <summary>
        /// Gets or sets the <see cref="T:LeanCloud.Realtime.AVIMTypedMessage"/> with the specified key.
        /// </summary>
        /// <param name="key">Key.</param>
        public virtual object this[string key]
        {
            get
            {
                if (estimatedData.TryGetValue(key, out object value)) {
                    return value;
                }
                return null;
            }
            set
            {
                estimatedData[key] = value;
            }
        }

        /// <summary>
        /// Merges the custom attributes.
        /// </summary>
        /// <param name="customAttributes">Custom attributes.</param>
        public void MergeCustomAttributes(IDictionary<string, object> customAttributes)
        {
            this.estimatedData = this.estimatedData.Merge(customAttributes);
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return estimatedData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
        }
    }

    /// <summary>
    /// AVIMMessage decorator.
    /// </summary>
    public abstract class AVIMMessageDecorator : AVIMTypedMessage
    {
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>The message.</value>
        public AVIMTypedMessage Message { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LeanCloud.Realtime.AVIMMessageDecorator"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        protected AVIMMessageDecorator(AVIMTypedMessage message)
        {
            this.Message = message;
        }

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        /// <value>The content of the message.</value>
        public virtual IDictionary<string, object> MessageContent { get; set; }

        /// <summary>
        /// Encodes the decorated.
        /// </summary>
        /// <returns>The decorated.</returns>
        public virtual IDictionary<string, object> EncodeDecorated()
        {
            return Message.Encode();
        }

        /// <summary>
        /// Encode this instance.
        /// </summary>
        /// <returns>The encode.</returns>
        public override IDictionary<string, object> Encode()
        {
            var decoratedMessageEncoded = EncodeDecorated();
            var selfEncoded = base.Encode();
            var decoratoEncoded = this.EncodeDecorator();
            var resultEncoed = decoratedMessageEncoded.Merge(selfEncoded).Merge(decoratoEncoded);
            return resultEncoed;
        }

        /// <summary>
        /// Encodes the decorator.
        /// </summary>
        /// <returns>The decorator.</returns>
        public abstract IDictionary<string, object> EncodeDecorator();
    }


}
