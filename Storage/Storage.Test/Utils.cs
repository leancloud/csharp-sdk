using System;
using LeanCloud;
using NUnit.Framework;

namespace LeanCloudTests {
    public static class Utils {
        public static void InitNorthChina(bool master = false) {
            if (master) {
                Init("BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz", "pbf6Nk5seyjilexdpyrPwjSp", "https://avoscloud.com", "https://avoscloud.com", "qKH9ryRagHKvXeRRVkiUiHeb");
            } else {
                Init("BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz", "pbf6Nk5seyjilexdpyrPwjSp", "https://avoscloud.com", "https://avoscloud.com");
            }
        }

        public static void InitEastChina(bool master = false) {
            if (master) {
                Init("4eTwHdYhMaNBUpl1SrTr7GLC-9Nh9j0Va", "GSD6DtdgGWlWolivN4qhWtlE", "https://4eTwHdYh.api.lncldapi.com", "https://4eTwHdYh.engine.lncldapi.com", "eqEp4n89h4zanWFskDDpIwL4");
            } else {
                Init("4eTwHdYhMaNBUpl1SrTr7GLC-9Nh9j0Va", "GSD6DtdgGWlWolivN4qhWtlE", "https://4eTwHdYh.api.lncldapi.com", "https://4eTwHdYh.engine.lncldapi.com");
            }
        }

        public static void InitUS(bool master = false) {
            if (master) {
                Init("MFAS1GnOyomRLSQYRaxdgdPz-MdYXbMMI", "p42JUxdxb95K5G8187t5ba3l", "https://MFAS1GnO.api.lncldglobal.com", "https://MFAS1GnO.engine.lncldglobal.com", "Ahb1wdFLwMgKwEaEicHRXbCY");
            } else {
                Init("MFAS1GnOyomRLSQYRaxdgdPz-MdYXbMMI", "p42JUxdxb95K5G8187t5ba3l", "https://MFAS1GnO.api.lncldglobal.com", "https://MFAS1GnO.engine.lncldglobal.com");
            }
        }

        static void Init(string appId, string appKey, string apiServer, string engineServer, string masterKey = null) {
            AVClient.Initialize(new AVClient.Configuration {
                ApplicationId = appId,
                ApplicationKey = appKey,
                MasterKey = masterKey,
                ApiServer = apiServer,
                EngineServer = engineServer,
            });
            AVClient.UseMasterKey = !string.IsNullOrEmpty(masterKey);
            AVClient.HttpLog(TestContext.Out.WriteLine);
        }
    }
}
