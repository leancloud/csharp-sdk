using System;
using System.Collections.Generic;

namespace LeanCloud.Storage {
    public class LCStatistic {
        public string Name {
            get; private set;
        }

        public double Value {
            get; private set;
        }

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
