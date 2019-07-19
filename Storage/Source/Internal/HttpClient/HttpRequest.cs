using System;
using System.Collections.Generic;
using System.IO;

namespace LeanCloud.Storage.Internal
{
    /// <summary>
    /// <code>IHttpRequest</code> is an interface that provides an API to execute HTTP request data.
    /// </summary>
    public class HttpRequest
    {
        public Uri Uri { get; set; }
        public IList<KeyValuePair<string, string>> Headers { get; set; }

        /// <summary>
        /// Data stream to be uploaded.
        /// </summary>
        public virtual Stream Data { get; set; }

        /// <summary>
        /// HTTP method. One of <c>DELETE</c>, <c>GET</c>, <c>HEAD</c>, <c>POST</c> or <c>PUT</c>
        /// </summary>
        public string Method { get; set; }
    }
}
