using System;
using System.Collections;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    public enum LCLeaderboardOrder {
        Ascending,
        Descending
    }

    public enum LCLeaderboardUpdateStrategy {
        Better,
        Last,
        Sum
    }

    public enum LCLeaderboardVersionChangeInterval {
        Never,
        Day,
        Week,
        Month
    }

    public class LCLeaderboard {
        public string StatisticName {
            get; private set;
        }

        public LCLeaderboardOrder Order {
            get; private set;
        }

        public LCLeaderboardUpdateStrategy UpdateStrategy {
            get; private set;
        }

        public LCLeaderboardVersionChangeInterval VersionChangeInterval {
            get; private set;
        }

        public int Version {
            get; private set;
        }

        public DateTime NextResetAt {
            get; private set;
        }

        public DateTime CreatedAt {
            get; private set;
        }

        public static async Task<LCLeaderboard> CreateLeaderboard(string statisticName,
            LCLeaderboardOrder order = LCLeaderboardOrder.Descending,
            LCLeaderboardUpdateStrategy updateStrategy = LCLeaderboardUpdateStrategy.Better,
            LCLeaderboardVersionChangeInterval versionChangeInterval = LCLeaderboardVersionChangeInterval.Week) {
            if (string.IsNullOrEmpty(statisticName)) {
                throw new ArgumentNullException(nameof(statisticName));
            }
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "statisticName", statisticName },
                { "order", order.ToString().ToLower() },
                { "versionChangeInterval", versionChangeInterval.ToString().ToLower() },
                { "updateStrategy", updateStrategy.ToString().ToLower() },
            };
            string path = "leaderboard/leaderboards";
            Dictionary<string, object> result = await LCInternalApplication.HttpClient.Post<Dictionary<string, object>>(path,
                data:data);
            LCLeaderboard leaderboard = new LCLeaderboard();
            leaderboard.Merge(result);
            return leaderboard;
        }

        /// <summary>
        /// Creates an empty leaderboard with a specified name.
        /// </summary>
        /// <param name="statisticName"></param>
        /// <returns></returns>
        public static LCLeaderboard CreateWithoutData(string statisticName) {
            if (string.IsNullOrEmpty(statisticName)) {
                throw new ArgumentNullException(nameof(statisticName));
            }
            return new LCLeaderboard {
                StatisticName = statisticName
            };
        }

        public static Task<LCLeaderboard> GetLeaderboard(string statisticName) {
            LCLeaderboard leaderboard = CreateWithoutData(statisticName);
            return leaderboard.Fetch();
        }


        public static async Task<ReadOnlyCollection<LCStatistic>> UpdateStatistics(LCUser user,
            Dictionary<string, double> statistics,
            bool overwrite = false) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (statistics == null || statistics.Count == 0) {
                throw new ArgumentNullException(nameof(statistics));
            }
            List<Dictionary<string, object>> data = statistics.Select(statistic => new Dictionary<string, object> {
                { "statisticName", statistic.Key },
                { "statisticValue", statistic.Value },
            }).ToList();
            string path = $"leaderboard/users/{user.ObjectId}/statistics";
            if (overwrite) {
                path = $"{path}?overwrite=1";
            }
            Dictionary<string, object> result = await LCInternalApplication.HttpClient.Post<Dictionary<string, object>>(path,
                data: data);
            if (result.TryGetValue("results", out object results) &&
                results is List<object> list) {
                List<LCStatistic> statisticList = new List<LCStatistic>();
                foreach (object item in list) {
                    LCStatistic statistic = LCStatistic.Parse(item as IDictionary<string, object>);
                    statisticList.Add(statistic);
                }
                return statisticList.AsReadOnly();
            }
            return null;
        }


        public static async Task<ReadOnlyCollection<LCStatistic>> GetStatistics(LCUser user,
            IEnumerable<string> statisticNames = null) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            string path = $"leaderboard/users/{user.ObjectId}/statistics";
            if (statisticNames != null && statisticNames.Count() > 0) {
                string names = string.Join(",", statisticNames);
                path = $"{path}?statistics={names}";
            }
            Dictionary<string, object> result = await LCInternalApplication.HttpClient.Get<Dictionary<string, object>>(path);
            if (result.TryGetValue("results", out object results) &&
                results is List<object> list) {
                List<LCStatistic> statistics = new List<LCStatistic>();
                foreach (object item in list) {
                    LCStatistic statistic = LCStatistic.Parse(item as Dictionary<string, object>);
                    statistics.Add(statistic);
                }
                return statistics.AsReadOnly();
            }
            return null;
        }

 
        public static async Task DeleteStatistics(LCUser user,
            IEnumerable<string> statisticNames) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (statisticNames == null || statisticNames.Count() == 0) {
                throw new ArgumentNullException(nameof(statisticNames));
            }
            string names = string.Join(",", statisticNames);
            string path = $"leaderboard/users/{user.ObjectId}/statistics?statistics={names}";
            await LCInternalApplication.HttpClient.Delete(path);
        }

        /// <summary>
        /// Gets historical data of this leaderboard.
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<LCLeaderboardArchive>> GetArchives(int skip = 0,
            int limit = 10) {
            if (skip < 0) {
                throw new ArgumentOutOfRangeException(nameof(skip));
            }
            if (limit <= 0) {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }
            string path = $"leaderboard/leaderboards/{StatisticName}/archives?skip={skip}&limit={limit}";
            Dictionary<string, object> result = await LCInternalApplication.HttpClient.Get<Dictionary<string, object>>(path);
            if (result.TryGetValue("results", out object results) &&
                results is List<object> list) {
                List<LCLeaderboardArchive> archives = new List<LCLeaderboardArchive>();
                foreach (object item in list) {
                    if (item is IDictionary<string, object> dict) {
                        LCLeaderboardArchive archive = LCLeaderboardArchive.Parse(dict);
                        archives.Add(archive);
                    }
                }
                return archives.AsReadOnly();
            }
            return null;
        }


        public Task<ReadOnlyCollection<LCRanking>> GetResults(int version = -1,
            int skip = 0,
            int limit = 10,
            IEnumerable<string> selectUserKeys = null,
            IEnumerable<string> includeStatistics = null) {
            return GetResults(null, version, skip, limit, selectUserKeys, includeStatistics);
        }


        public async Task<ReadOnlyCollection<LCRanking>> GetResultsAroundUser(int version = -1,
            int skip = 0,
            int limit = 10,
            IEnumerable<string> selectUserKeys = null,
            IEnumerable<string> includeStatistics = null) {
            LCUser user = await LCUser.GetCurrent();
            return await GetResults(user, version, skip, limit, selectUserKeys, includeStatistics);
        }

        private async Task<ReadOnlyCollection<LCRanking>> GetResults(LCUser user,
            int version,
            int skip,
            int limit,
            IEnumerable<string> selectUserKeys,
            IEnumerable<string> includeStatistics) {
            string path = $"leaderboard/leaderboards/{StatisticName}/ranks";
            if (user != null) {
                path = $"{path}/{user.ObjectId}";
            }
            path = $"{path}?skip={skip}&limit={limit}";
            if (version != -1) {
                path = $"{path}&version={version}";
            }
            if (selectUserKeys != null) {
                string keys = string.Join(",", selectUserKeys);
                path = $"{path}&includeUser={keys}";
            }
            if (includeStatistics != null) {
                string statistics = string.Join(",", includeStatistics);
                path = $"{path}&includeStatistics={statistics}";
            }
            Dictionary<string, object> result = await LCInternalApplication.HttpClient.Get<Dictionary<string, object>>(path);
            if (result.TryGetValue("results", out object results) &&
                results is List<object> list) {
                List<LCRanking> rankings = new List<LCRanking>();
                foreach (object item in list) {
                    LCRanking ranking = LCRanking.Parse(item as IDictionary<string, object>);
                    rankings.Add(ranking);
                }
                return rankings.AsReadOnly();
            }
            return null;
        }


        public async Task<LCLeaderboard> UpdateUpdateStrategy(LCLeaderboardUpdateStrategy updateStrategy) {
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "updateStrategy", updateStrategy.ToString().ToLower() }
            };
            string path = $"leaderboard/leaderboards/{StatisticName}";
            Dictionary<string, object> result = await LCInternalApplication.HttpClient.Put<Dictionary<string, object>>(path,
                data: data);
            if (result.TryGetValue("updateStrategy", out object strategy) &&
                Enum.TryParse(strategy as string, true, out LCLeaderboardUpdateStrategy s)) {
                UpdateStrategy = s;
            }
            return this;
        }


        public async Task<LCLeaderboard> UpdateVersionChangeInterval(LCLeaderboardVersionChangeInterval versionChangeInterval) {
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "versionChangeInterval", versionChangeInterval.ToString().ToLower() }
            };
            string path = $"leaderboard/leaderboards/{StatisticName}";
            Dictionary<string, object> result = await LCInternalApplication.HttpClient.Put<Dictionary<string, object>>(path,
                data: data);
            if (result.TryGetValue("versionChangeInterval", out object interval) &&
                Enum.TryParse(interval as string, true, out LCLeaderboardVersionChangeInterval i)) {
                VersionChangeInterval = i;
            }
            return this;
        }

        /// <summary>
        /// Fetches leaderboard data.
        /// </summary>
        /// <returns></returns>
        public async Task<LCLeaderboard> Fetch() {
            string path = $"leaderboard/leaderboards/{StatisticName}";
            Dictionary<string, object> result = await LCInternalApplication.HttpClient.Get<Dictionary<string, object>>(path);
            Merge(result);
            return this;
        }

        /// <summary>
        /// Resets this leaderboard.
        /// </summary>
        /// <returns></returns>
        public async Task<LCLeaderboard> Reset() {
            string path = $"leaderboard/leaderboards/{StatisticName}/incrementVersion";
            Dictionary<string, object> result = await LCInternalApplication.HttpClient.Put<Dictionary<string, object>>(path);
            Merge(result);
            return this;
        }

        /// <summary>
        /// Destroys this leaderboard.
        /// </summary>
        /// <returns></returns>
        public async Task Destroy() {
            string path = $"leaderboard/leaderboards/{StatisticName}";
            await LCInternalApplication.HttpClient.Delete(path);
        }

        private void Merge(Dictionary<string, object> data) {
            if (data.TryGetValue("statisticName", out object statisticName)) {
                StatisticName = statisticName as string;
            }
            if (data.TryGetValue("order", out object order) &&
                Enum.TryParse(order as string, true, out LCLeaderboardOrder o)) {
                Order = o;
            }
            if (data.TryGetValue("updateStrategy", out object strategy) &&
                Enum.TryParse(strategy as string, true, out LCLeaderboardUpdateStrategy s)) {
                UpdateStrategy = s;
            }
            if (data.TryGetValue("versionChangeInterval", out object interval) &&
                Enum.TryParse(interval as string, true, out LCLeaderboardVersionChangeInterval i)) {
                VersionChangeInterval = i;
            }
            if (data.TryGetValue("version", out object version)) {
                Version = Convert.ToInt32(version);
            }
            if (data.TryGetValue("createdAt", out object createdAt) &&
                createdAt is DateTime dt) {
                CreatedAt = dt;
            }
            if (data.TryGetValue("expiredAt", out object expiredAt) &&
                expiredAt is IDictionary dict) {
                NextResetAt = LCDecoder.DecodeDate(dict);
            }
        }
    }
}
