using System;
using System.Collections.Generic;
using LeanCloud.Storage.Internal;

namespace LeanCloud {
    /// <summary>
    /// 排名类
    /// </summary>
    public class AVRanking {
        /// <summary>
        /// 名次
        /// </summary>
        /// <value>The rank.</value>
        public int Rank {
            get; private set;
        }

        /// <summary>
        /// 用户
        /// </summary>
        /// <value>The user.</value>
        public AVUser User {
            get; private set;
        }

        public string StatisticName {
            get; private set;
        }

        /// <summary>
        /// 分数
        /// </summary>
        /// <value>The value.</value>
        public double Value {
            get; private set;
        }

        /// <summary>
        /// 成绩
        /// </summary>
        /// <value>The included statistics.</value>
        public List<AVStatistic> IncludedStatistics {
            get; private set;
        }

        AVRanking() {
        }

        internal static AVRanking Parse(IDictionary<string, object> data) {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }
            var ranking = new AVRanking {
                Rank = int.Parse(data["rank"].ToString()),
                User = AVDecoder.Instance.Decode(data["user"]) as AVUser,
                StatisticName = data["statisticName"].ToString(),
                Value = double.Parse(data["statisticValue"].ToString())
            };
            object statisticsObj;
            if (data.TryGetValue("statistics", out statisticsObj)) {
                ranking.IncludedStatistics = new List<AVStatistic>();
                var statisticsObjList = statisticsObj as List<object>;
                foreach (object statisticObj in statisticsObjList) {
                    var statistic = AVStatistic.Parse(statisticObj as IDictionary<string, object>);
                    ranking.IncludedStatistics.Add(statistic);
                }
            }

            return ranking;
        }
    }
}
