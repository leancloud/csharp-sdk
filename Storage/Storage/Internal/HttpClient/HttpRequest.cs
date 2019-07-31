using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace LeanCloud.Storage.Internal
{
    /// <summary>
    /// <code>IHttpRequest</code> is an interface that provides an API to execute HTTP request data.
    /// </summary>
    public class HttpRequest
    {
        public Uri Uri { get; set; }

        public IList<KeyValuePair<string, string>> Headers { get; set; }

        // HttpMethod
        public HttpMethod Method { get; set; }

        public virtual Stream Data { get; set; }
    }
}
