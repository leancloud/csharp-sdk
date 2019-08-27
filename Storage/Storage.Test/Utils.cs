using System;
using LeanCloud;
using NUnit.Framework;

namespace LeanCloudTests {
    public static class Utils {
        public static void InitNorthChina() {
            AVClient.Initialize(new AVClient.Configuration {
                ApplicationId = "BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz",
                ApplicationKey = "pbf6Nk5seyjilexdpyrPwjSp",
                ApiServer = "https://avoscloud.com"
            });
            AVClient.HttpLog(TestContext.Out.WriteLine);
        }

        public static void InitEastChina() {
            AVClient.Initialize(new AVClient.Configuration {
                ApplicationId = "4eTwHdYhMaNBUpl1SrTr7GLC-9Nh9j0Va",
                ApplicationKey = "GSD6DtdgGWlWolivN4qhWtlE",
                ApiServer = "https://4eTwHdYh.api.lncldapi.com"
            });
            AVClient.HttpLog(TestContext.Out.WriteLine);
        }

        public static void InitUS() {
            AVClient.Initialize(new AVClient.Configuration {
                ApplicationId = "MFAS1GnOyomRLSQYRaxdgdPz-MdYXbMMI",
                ApplicationKey = "p42JUxdxb95K5G8187t5ba3l",
                ApiServer = "https://MFAS1GnO.api.lncldglobal.com"
            });
            AVClient.HttpLog(TestContext.Out.WriteLine);
        }
    }
}
