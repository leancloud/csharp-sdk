using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    public static class LCFriendship {
        public static async Task Request(string userId, Dictionary<string, object> attributes = null) {
            LCUser user = await LCUser.GetCurrent();
            if (user == null) {
                throw new ArgumentNullException("current user");
            }
            string path = "users/friendshipRequests";
            LCObject friend = LCObject.CreateWithoutData("_User", userId);
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "user", LCEncoder.EncodeLCObject(user) },
                { "friend", LCEncoder.EncodeLCObject(friend) },
            };
            if (attributes != null) {
                data["friendship"] = attributes;
            }
            await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }

        public static async Task AcceptRequest(LCFriendshipRequest request, Dictionary<string, object> attributes = null) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            string path = $"users/friendshipRequests/{request.ObjectId}/accept";
            Dictionary<string, object> data = null;
            if (attributes != null) {
                data = new Dictionary<string, object> {
                    { "friendship", attributes }
                };
            }
            await LCApplication.HttpClient.Put<Dictionary<string, object>>(path, data: data);
        }

        public static async Task DeclineRequest(LCFriendshipRequest request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            string path = $"users/friendshipRequests/{request.ObjectId}/decline";
            await LCApplication.HttpClient.Put<Dictionary<string, object>>(path);
        }
    }
}
