using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LeanCloud.Storage.Internal {
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
            get; set;
        }

        public object Content {
            get; set;
        }

        internal Uri Uri {
            get {
                return new Uri($"{Server}/{AVClient.APIVersion}/{Path}");
            }
        }
    }
}
