using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LeanCloud.Storage.Internal;
using System.Linq;
using System.Net.Http;

namespace LeanCloud.Storage.Internal
{
    /// <summary>
    /// AVCommand is an <see cref="HttpRequest"/> with pre-populated
    /// headers.
    /// </summary>
    public class AVCommand {
        // 不同服务对应的服务器地址不同
        public virtual string Server => AVClient.CurrentConfiguration.ApiServer;

        public string Path {
            get; set;
        }

        public HttpMethod Method {
            get; set;
        }

        public Dictionary<string, string> Headers {
            get {
                if (AVUser.CurrentUser != null) {
                    return new Dictionary<string, string> {
                        { "X-LC-Session", AVUser.CurrentUser.SessionToken }
                    };
                }
                return null;
            }
        }

        public object Content {
            get; set;
        }

        public Uri Uri {
            get {
                return new Uri($"{Server}/{AVClient.APIVersion}/{Path}");
            }
        }
    }
}
