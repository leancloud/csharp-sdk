using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LeanCloud.Storage.Internal;
using System.Linq;

namespace LeanCloud.Storage.Internal
{
    /// <summary>
    /// AVCommand is an <see cref="HttpRequest"/> with pre-populated
    /// headers.
    /// </summary>
    public class AVCommand : HttpRequest
    {
        public IDictionary<string, object> DataObject { get; private set; }
        public override Stream Data
        {
            get
            {
                if (base.Data != null)
                {
                    return base.Data;
                }

                return base.Data = (DataObject != null
                  ? new MemoryStream(Encoding.UTF8.GetBytes(Json.Encode(DataObject)))
                  : null);
            }
            set { base.Data = value; }
        }

        public AVCommand(string relativeUri,
            string method,
            string sessionToken = null,
            IList<KeyValuePair<string, string>> headers = null,
            IDictionary<string, object> data = null) : this(relativeUri: relativeUri,
                method: method,
                sessionToken: sessionToken,
                headers: headers,
                stream: null,
                contentType: data != null ? "application/json" : null)
        {
            DataObject = data;
        }

        public AVCommand(string relativeUri,
                string method,
                string sessionToken = null,
                IList<KeyValuePair<string, string>> headers = null,
                Stream stream = null,
                string contentType = null)
        {
            var state = AVPlugins.Instance.AppRouterController.Get();
            var urlTemplate = "https://{0}/{1}/{2}";
            AVClient.Configuration configuration = AVClient.CurrentConfiguration;
            var apiVersion = "1.1";
            if (relativeUri.StartsWith("push") || relativeUri.StartsWith("installations"))
            {
                Uri = new Uri(string.Format(urlTemplate, state.PushServer, apiVersion, relativeUri));
                if (configuration.PushServer != null)
                {
                    Uri = new Uri(string.Format("{0}{1}/{2}", configuration.PushServer, apiVersion, relativeUri));
                }
            }
            else if (relativeUri.StartsWith("stats") || relativeUri.StartsWith("always_collect") || relativeUri.StartsWith("statistics"))
            {
                Uri = new Uri(string.Format(urlTemplate, state.StatsServer, apiVersion, relativeUri));
                if (configuration.StatsServer != null)
                {
                    Uri = new Uri(string.Format("{0}{1}/{2}", configuration.StatsServer, apiVersion, relativeUri));
                }
            }
            else if (relativeUri.StartsWith("functions") || relativeUri.StartsWith("call"))
            {
                Uri = new Uri(string.Format(urlTemplate, state.EngineServer, apiVersion, relativeUri));

                if (configuration.EngineServer != null)
                {
                    Uri = new Uri(string.Format("{0}{1}/{2}", configuration.EngineServer, apiVersion, relativeUri));
                }
            }
            else
            {
                Uri = new Uri(string.Format(urlTemplate, state.ApiServer, apiVersion, relativeUri));

                if (configuration.ApiServer != null)
                {
                    Uri = new Uri(string.Format("{0}{1}/{2}", configuration.ApiServer, apiVersion, relativeUri));
                }
            }
            Method = method;
            Data = stream;
            Headers = new List<KeyValuePair<string, string>>(headers ?? Enumerable.Empty<KeyValuePair<string, string>>());

            string useProduction = AVClient.UseProduction ? "1" : "0";
            Headers.Add(new KeyValuePair<string, string>("X-LC-Prod", useProduction));

            if (!string.IsNullOrEmpty(sessionToken))
            {
                Headers.Add(new KeyValuePair<string, string>("X-LC-Session", sessionToken));
            }
            if (!string.IsNullOrEmpty(contentType))
            {
                Headers.Add(new KeyValuePair<string, string>("Content-Type", contentType));
            }
        }

        public AVCommand(AVCommand other)
        {
            this.Uri = other.Uri;
            this.Method = other.Method;
            this.DataObject = other.DataObject;
            this.Headers = new List<KeyValuePair<string, string>>(other.Headers);
            this.Data = other.Data;
        }
    }
}
