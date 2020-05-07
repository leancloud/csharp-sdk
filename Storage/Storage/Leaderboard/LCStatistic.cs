using System;
using System.Collections.Generic;

namespace LeanCloud.Storage {
    /// <summary>
    /// 成绩
    /// </summary>
    public class LCStatistic {
        /// <summary>
        /// 排行榜名字
        /// </summary>
        public string Name {
            get; private set;
        }

        /// <summary>
        /// 成绩值
        /// </summary>
        public double Value {
            get; private set;
        }

        /// <summary>
        /// 排行榜版本
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
