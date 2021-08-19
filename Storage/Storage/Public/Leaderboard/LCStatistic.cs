using System;
using System.Collections.Generic;
using System.Collections;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage.Internal.Codec;

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

        /// <summary>
        /// The user of this LCRanking.
        /// </summary>
        public LCUser User {
            get; private set;
        }

        /// <summary>
        /// The object of this LCRanking.
        /// </summary>
        public LCObject Object {
            get; private set;
        }

        /// <summary>
        /// The entity of this LCRanking.
        /// </summary>
        public string Entity {
            get; private set;
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
            if (data.TryGetValue("user", out object user)) {
                LCObjectData objectData = LCObjectData.Decode(user as IDictionary);
                statistic.User = LCUser.GenerateUser(objectData);
            }
            if (data.TryGetValue("object", out object obj)) {
                statistic.Object = LCDecoder.DecodeObject(obj as IDictionary);
            }
            if (data.TryGetValue("entity", out object entity)) {
                statistic.Entity = entity as string;
            }
            return statistic;
        }
    }
}
