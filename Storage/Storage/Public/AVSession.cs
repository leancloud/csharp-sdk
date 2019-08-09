using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud
{
    /// <summary>
    /// Represents a session of a user for a LeanCloud application.
    /// </summary>
    [AVClassName("_Session")]
    public class AVSession : AVObject
    {
        private static readonly HashSet<string> readOnlyKeys = new HashSet<string> {
            "sessionToken", "createdWith", "restricted", "user", "expiresAt", "installationId"
        };

        protected override bool IsKeyMutable(string key)
        {
            return !readOnlyKeys.Contains(key);
        }

        /// <summary>
        /// Gets the session token for a user, if they are logged in.
        /// </summary>
        [AVFieldName("sessionToken")]
        public string SessionToken
        {
            get { return GetProperty<string>(null, "SessionToken"); }
        }

        /// <summary>
        /// Constructs a <see cref="AVQuery{AVSession}"/> for AVSession.
        /// </summary>
        public static AVQuery<AVSession> Query
        {
            get
            {
                return new AVQuery<AVSession>();
            }
        }

        internal static AVSessionController SessionController
        {
            get
            {
                return AVPlugins.Instance.SessionController;
            }
        }

        /// <summary>
        /// Gets the current <see cref="AVSession"/> object related to the current user.
        /// </summary>
        public static Task<AVSession> GetCurrentSessionAsync()
        {
            return GetCurrentSessionAsync(CancellationToken.None);
        }

        /// <summary>
        /// Gets the current <see cref="AVSession"/> object related to the current user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task<AVSession> GetCurrentSessionAsync(CancellationToken cancellationToken)
        {
            AVUser user = AVUser.CurrentUser;
            if (user == null) {
                return Task<AVSession>.FromResult((AVSession)null);
            }

            string sessionToken = user.SessionToken;
            if (sessionToken == null) {
                return Task<AVSession>.FromResult((AVSession)null);
            }

            return SessionController.GetSessionAsync(sessionToken, cancellationToken).OnSuccess(t =>
            {
                AVSession session = AVObject.FromState<AVSession>(t.Result, "_Session");
                return session;
            });
        }

        internal static Task RevokeAsync(string sessionToken, CancellationToken cancellationToken)
        {
            if (sessionToken == null || !SessionController.IsRevocableSessionToken(sessionToken))
            {
                return Task.FromResult(0);
            }
            return SessionController.RevokeAsync(sessionToken, cancellationToken);
        }

        internal static Task<string> UpgradeToRevocableSessionAsync(string sessionToken, CancellationToken cancellationToken)
        {
            if (sessionToken == null || SessionController.IsRevocableSessionToken(sessionToken))
            {
                return Task<string>.FromResult(sessionToken);
            }

            return SessionController.UpgradeToRevocableSessionAsync(sessionToken, cancellationToken).OnSuccess(t =>
            {
                AVSession session = AVObject.FromState<AVSession>(t.Result, "_Session");
                return session.SessionToken;
            });
        }
    }
}
