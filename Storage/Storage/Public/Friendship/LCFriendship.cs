using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Common;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    /// <summary>
    /// LCFriendship contains static functions that handle LCFriendship.
    /// </summary>
    public static class LCFriendship {
        /// <summary>
        /// Requests to add a friend.
        /// </summary>
        /// <param name="userId">The user id to add.</param>
        /// <param name="attributes">The additional attributes for the friendship.</param>
        /// <returns></returns>
        public static async Task Request(string userId, Dictionary<string, object> attributes = null) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }

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
            await LCCore.HttpClient.Post<Dictionary<string, object>>(path, data: data);
        }

        /// <summary>
        /// Accepts a friend request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="attributes">The additional attributes for the friendship.</param>
        /// <returns></returns>
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
            await LCCore.HttpClient.Put<Dictionary<string, object>>(path, data: data);
        }

        /// <summary>
        /// Declines a friend request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public static async Task DeclineRequest(LCFriendshipRequest request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            string path = $"users/friendshipRequests/{request.ObjectId}/decline";
            await LCCore.HttpClient.Put<Dictionary<string, object>>(path);
        }

        /// <summary>
        /// Blocks the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static async Task Block(string userId) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }

            LCUser user = await LCUser.GetCurrent();
            if (user == null) {
                throw new ArgumentNullException("current user");
            }

            string path = $"users/self/friendBlocklist/{userId}";
            await LCCore.HttpClient.Post<object>(path);
        }

        /// <summary>
        /// Unblocks the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static async Task Unblock(string userId) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }

            LCUser user = await LCUser.GetCurrent();
            if (user == null) {
                throw new ArgumentNullException("current user");
            }

            string path = $"users/self/friendBlocklist/{userId}";
            await LCCore.HttpClient.Delete(path);
        }
    }
}
