using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LeanCloud.Storage.Internal
{
    /// <summary>
    /// A <see cref="AVEncoder"/> that encode <see cref="AVObject"/> as pointers. If the object
    /// does not have an <see cref="AVObject.ObjectId"/>, uses a local id.
    /// </summary>
    public class PointerOrLocalIdEncoder : AVEncoder
    {
        // This class isn't really a Singleton, but since it has no state, it's more efficient to get
        // the default instance.
        private static readonly PointerOrLocalIdEncoder instance = new PointerOrLocalIdEncoder();
        public static PointerOrLocalIdEncoder Instance
        {
            get
            {
                return instance;
            }
        }

        protected override IDictionary<string, object> EncodeAVObject(AVObject value)
        {
            if (value.ObjectId == null)
            {
                // TODO (hallucinogen): handle local id. For now we throw.
                throw new ArgumentException("Cannot create a pointer to an object without an objectId");
            }

            return new Dictionary<string, object> {
                {"__type", "Pointer"},
                { "className", value.ClassName},
                { "objectId", value.ObjectId}
            };
        }

        public IDictionary<string, object> EncodeAVObject(AVObject value, bool isPointer)
        {
            if (isPointer)
            {
                return EncodeAVObject(value);
            }
            var operations = value.GetCurrentOperations();
            var operationJSON = AVObject.ToJSONObjectForSaving(operations);
            var objectJSON = value.ToDictionary(kvp => kvp.Key, kvp => PointerOrLocalIdEncoder.Instance.Encode(kvp.Value));
            foreach (var kvp in operationJSON) {
                objectJSON[kvp.Key] = kvp.Value;
            }
            if (value.CreatedAt.HasValue) {
                objectJSON["createdAt"] = value.CreatedAt.Value.ToString(AVClient.DateFormatStrings.First(), CultureInfo.InvariantCulture);
            }
            if (value.UpdatedAt.HasValue) {
                objectJSON["updatedAt"] = value.UpdatedAt.Value.ToString(AVClient.DateFormatStrings.First(), CultureInfo.InvariantCulture);
            }
            if(!string.IsNullOrEmpty(value.ObjectId)) {
                objectJSON["objectId"] = value.ObjectId;
            }
            if (value.ACL != null) {
                objectJSON["acl"] = Encode(value.ACL);
            }
            objectJSON["className"] = value.ClassName;
            objectJSON["__type"] = "Object";
            return objectJSON;
        }
    }
}
