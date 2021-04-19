using System;
using System.Collections.Generic;

namespace LeanCloud.Storage {
    /// <summary>
    /// LCStatistic represents the statistic of LeanCloud leaderboard.
    /// </summary>
    public class LCStatistic {
        /// <summary>
        /// The name of this LCStatistic.
        /// </summary>
        public string Name {
            get; private set;
        }

        /// <summary>
        /// The value of this LCStatistic.
        /// </summary>
        public double Value {
            get; private set;
        }

        /// <summary>
        /// The version of this LCStatistic.
        /// </summary>
        public int Version {
            get; internal set;
        }

        internal static LCStatistic Parse(IDictionary<string, object> data) {
            LCStatistic statistic = new LCStatistic();
            if (data.TryGetValue("statisticName", out object statisticName)) {
                statistic.Name = statisticName as string;
            }
            if (data.TryGetValue("statisticValue", out object value)) {
                statistic.Value = Convert.ToDouble(value);
            }
            if (data.TryGetValue("version", out object version)) {
                statistic.Version = Convert.ToInt32(version);
            }
            return statistic;
        }
    }
}
