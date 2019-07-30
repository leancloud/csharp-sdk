using System;
using System.Collections.Generic;
using LeanCloud.Storage.Internal;

namespace LeanCloud {
    /// <summary>
    /// 归档的排行榜
    /// </summary>
    public class AVLeaderboardArchive {
        /// <summary>
        /// 名称
        /// </summary>
        /// <value>The name of the statistic.</value>
        public string StatisticName {
            get; internal set;
        }

        /// <summary>
        /// 版本号
        /// </summary>
        /// <value>The version.</value>
        public int Version {
            get; internal set;
        }

        /// <summary>
        /// 状态
        /// </summary>
        /// <value>The status.</value>
        public string Status {
            get; internal set;
        }

        /// <summary>
        /// 下载地址
        /// </summary>
        /// <value>The URL.</value>
        public string Url {
            get; internal set;
        }

        /// <summary>
        /// 激活时间
        /// </summary>
        /// <value>The activated at.</value>
        public DateTime ActivatedAt {
            get; internal set;
        }

        /// <summary>
        /// 归档时间
        /// </summary>
        /// <value>The deactivated at.</value>
        public DateTime DeactivatedAt {
            get; internal set;
        }

        AVLeaderboardArchive() {
        }

        internal static AVLeaderboardArchive Parse(IDictionary<string, object> data) {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }
            AVLeaderboardArchive archive = new AVLeaderboardArchive {
                StatisticName = data["statisticName"].ToString(),
                Version = int.Parse(data["version"].ToString()),
                Status = data["status"].ToString(),
                Url = data["url"].ToString(),
                ActivatedAt = (DateTime)AVDecoder.Instance.Decode(data["activatedAt"]),
                DeactivatedAt = (DateTime)AVDecoder.Instance.Decode(data["activatedAt"])
            };
            return archive;
        }
    }
}
