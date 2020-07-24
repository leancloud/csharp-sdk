using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LeanCloud.Storage.Internal.Object;

namespace LeanCloud.Storage {
    public class LCRanking {
        public int Rank {
            get; private set;
        }

        public LCUser User {
            get; private set;
        }

        public string StatisticName {
            get; private set;
        }

        public double Value {
            get; private set;
        }
        
        public ReadOnlyCollection<LCStatistic> IncludedStatistics {
            get; private set;
        }

        internal static LCRanking Parse(IDictionary<string, object> data) {
            LCRanking ranking = new LCRanking();
            if (data.TryGetValue("rank", out object rank)) {
                ranking.Rank = Convert.ToInt32(rank);
            }
            if (data.TryGetValue("user", out object user)) {
                LCObjectData objectData = LCObjectData.Decode(user as System.Collections.IDictionary);
                ranking.User = new LCUser(objectData);
            }
            if (data.TryGetValue("statisticName", out object statisticName)) {
                ranking.StatisticName = statisticName as string;
            }
            if (data.TryGetValue("statisticValue", out object value)) {
                ranking.Value = Convert.ToDouble(value);
            }
            if (data.TryGetValue("statistics", out object statistics) &&
                statistics is List<object> list) {
                List<LCStatistic> statisticList = new List<LCStatistic>();
                foreach (object item in list) {
                    LCStatistic statistic = LCStatistic.Parse(item as IDictionary<string, object>);
                    statisticList.Add(statistic);
                }
                ranking.IncludedStatistics = statisticList.AsReadOnly();
            }
            return ranking;
        }
    }
}
