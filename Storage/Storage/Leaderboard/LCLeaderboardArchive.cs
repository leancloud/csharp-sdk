using System;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    /// <summary>
    /// 归档的排行榜
    /// </summary>
    public class LCLeaderboardArchive {
        /// <summary>
        /// 名称
        /// </summary>
        public string StatisticName {
            get; internal set;
        }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version {
            get; internal set;
        }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status {
            get; internal set;
        }

        /// <summary>
        /// 下载地址
        /// </summary>
        public string Url {
            get; internal set;
        }

        /// <summary>
        /// 激活时间
        /// </summary>
        public DateTime ActivatedAt {
            get; internal set;
        }

        /// <summary>
        /// 归档时间
        /// </summary>
        public DateTime DeactivatedAt {
            get; internal set;
        }

        internal static LCLeaderboardArchive Parse(IDictionary<string, object> data) {
            LCLeaderboardArchive archive = new LCLeaderboardArchive();
            if (data.TryGetValue("statisticName", out object statisticName)) {
                archive.StatisticName = statisticName as string;
            }
            if (data.TryGetValue("version", out object version)) {
                archive.Version = Convert.ToInt32(version);
            }
            if (data.TryGetValue("status", out object status)) {
                archive.Status = status as string;
            }
            if (data.TryGetValue("url", out object url)) {
                archive.Url = url as string;
            }
            if (data.TryGetValue("activatedAt", out object activatedAt) &&
                activatedAt is System.Collections.IDictionary actDt) {
                archive.ActivatedAt = LCDecoder.DecodeDate(actDt);
            }
            if (data.TryGetValue("deactivatedAt", out object deactivatedAt) &&
                deactivatedAt is System.Collections.IDictionary deactDt) {
                archive.DeactivatedAt = LCDecoder.DecodeDate(deactDt);
            }
            return archive;
        }
    }
}
