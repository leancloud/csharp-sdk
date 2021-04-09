using System;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    /// <summary>
    /// Archived leaderboard.
    /// </summary>
    public class LCLeaderboardArchive {
        public string StatisticName {
            get; internal set;
        }

        public int Version {
            get; internal set;
        }

        /// <summary>
        /// Archive status. One of scheduled, inProgress, failed, completed.
        /// </summary>
        public string Status {
            get; internal set;
        }
        
        /// <summary>
        /// Download URL of the archived leaderboard file.
        /// </summary>
        public string Url {
            get; internal set;
        }

        public DateTime ActivatedAt {
            get; internal set;
        }

        /// <summary>
        /// Archive time.
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
