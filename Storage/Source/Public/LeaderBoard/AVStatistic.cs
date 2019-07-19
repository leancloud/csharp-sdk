using System;
using System.Collections.Generic;

namespace LeanCloud {
    /// <summary>
    /// 成绩类
    /// </summary>
    public class AVStatistic {
        /// <summary>
        /// 排行榜名称
        /// </summary>
        /// <value>The name.</value>
        public string Name {
            get; private set;
        }

        /// <summary>
        /// 成绩值
        /// </summary>
        /// <value>The value.</value>
        public double Value {
            get; private set;
        }

        /// <summary>
        /// 排行榜版本
        /// </summary>
        /// <value>The version.</value>
        public int Version {
            get; internal set;
        }

        public AVStatistic(string name, double value) {
            Name = name;
            Value = value;
        }

        AVStatistic() { }

        internal static AVStatistic Parse(IDictionary<string, object> data) {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }
            AVStatistic statistic = new AVStatistic {
                Name = data["statisticName"].ToString(),
                Value = double.Parse(data["statisticValue"].ToString()),
                Version = int.Parse(data["version"].ToString())
            };
            return statistic;
        }
    }
}
