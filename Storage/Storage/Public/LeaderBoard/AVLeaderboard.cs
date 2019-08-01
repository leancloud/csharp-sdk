using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Storage.Internal;
using System.Net.Http;

namespace LeanCloud {
    /// <summary>
    /// 排行榜顺序 
    /// </summary>
    public enum AVLeaderboardOrder {
        /// <summary>
        /// 升序
        /// </summary>
        ASCENDING,
        /// <summary>
        /// 降序
        /// </summary>
        DESCENDING
    }

    /// <summary>
    /// 排行榜更新策略 
    /// </summary>
    public enum AVLeaderboardUpdateStrategy {
        /// <summary>
        /// 更好地
        /// </summary>
        BETTER,
        /// <summary>
        /// 最近的
        /// </summary>
        LAST,
        /// <summary>
        /// 总和
        /// </summary>
        SUM,
    }

    /// <summary>
    /// 排行榜刷新频率
    /// </summary>
    public enum AVLeaderboardVersionChangeInterval {
        /// <summary>
        /// 从不
        /// </summary>
        NEVER,
        /// <summary>
        /// 每天
        /// </summary>
        DAY,
        /// <summary>
        /// 每周
        /// </summary>
        WEEK,
        /// <summary>
        /// 每月
        /// </summary>
        MONTH
    }

    /// <summary>
    /// 排行榜类
    /// </summary>
    public class AVLeaderboard {
        /// <summary>
        /// 成绩名字
        /// </summary>
        /// <value>The name of the statistic.</value>
        public string StatisticName {
            get; private set;
        }

        /// <summary>
        /// 排行榜顺序
        /// </summary>
        /// <value>The order.</value>
        public AVLeaderboardOrder Order {
            get; private set;
        }

        /// <summary>
        /// 排行榜更新策略
        /// </summary>
        /// <value>The update strategy.</value>
        public AVLeaderboardUpdateStrategy UpdateStrategy {
            get; private set;
        }

        /// <summary>
        /// 排行榜版本更新频率
        /// </summary>
        /// <value>The version change intervak.</value>
        public AVLeaderboardVersionChangeInterval VersionChangeInterval {
            get; private set;
        } 

        /// <summary>
        /// 版本号
        /// </summary>
        /// <value>The version.</value>
        public int Version {
            get; private set;
        }

        /// <summary>
        /// 下次重置时间
        /// </summary>
        /// <value>The next reset at.</value>
        public DateTime NextResetAt {
            get; private set;
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        /// <value>The created at.</value>
        public DateTime CreatedAt {
            get; private set;
        }

        /// <summary>
        /// Leaderboard 构造方法
        /// </summary>
        /// <param name="statisticName">成绩名称</param>
        AVLeaderboard(string statisticName) {
            StatisticName = statisticName;
        }

        AVLeaderboard() { 
        }

        /// <summary>
        /// 创建排行榜对象
        /// </summary>
        /// <returns>排行榜对象</returns>
        /// <param name="statisticName">名称</param>
        /// <param name="order">排序方式</param>
        /// <param name="versionChangeInterval">版本更新频率</param>
        /// <param name="updateStrategy">成绩更新策略</param>
        public static Task<AVLeaderboard> CreateLeaderboard(string statisticName, 
            AVLeaderboardOrder order = AVLeaderboardOrder.DESCENDING,
            AVLeaderboardUpdateStrategy updateStrategy = AVLeaderboardUpdateStrategy.BETTER,
            AVLeaderboardVersionChangeInterval versionChangeInterval = AVLeaderboardVersionChangeInterval.WEEK) {

            if (string.IsNullOrEmpty(statisticName)) {
                throw new ArgumentNullException(nameof(statisticName));
            }
            var data = new Dictionary<string, object> {
                { "statisticName", statisticName },
                { "order", order.ToString().ToLower() },
                { "versionChangeInterval", versionChangeInterval.ToString().ToLower() },
                { "updateStrategy", updateStrategy.ToString().ToLower() },
            };
            var command = new AVCommand {
                Path = "leaderboard/leaderboards",
                Method = HttpMethod.Post,
                Content = data
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t => {
                try {
                    var leaderboard = Parse(t.Result.Item2);
                    return leaderboard;
                } catch (Exception e) {
                    throw new AVException(AVException.ErrorCode.InvalidJSON, e.Message);
                }
            });
        }

        /// <summary>
        /// 创建排行榜对象
        /// </summary>
        /// <returns>排行榜对象</returns>
        /// <param name="statisticName">名称</param>
        public static AVLeaderboard CreateWithoutData(string statisticName) {
            if (string.IsNullOrEmpty(statisticName)) {
                throw new ArgumentNullException(nameof(statisticName));
            }
            return new AVLeaderboard(statisticName);
        }

        /// <summary>
        /// 获取排行榜对象
        /// </summary>
        /// <returns>排行榜对象</returns>
        /// <param name="statisticName">名称</param>
        public static Task<AVLeaderboard> GetLeaderboard(string statisticName) {
            return CreateWithoutData(statisticName).Fetch();
        }

        /// <summary>
        /// 更新用户成绩
        /// </summary>
        /// <returns>更新的成绩</returns>
        /// <param name="user">用户</param>
        /// <param name="statistics">成绩</param>
        /// <param name="overwrite">是否强行覆盖</param>
        public static Task<List<AVStatistic>> UpdateStatistics(AVUser user, Dictionary<string, double> statistics, bool overwrite = false) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (statistics == null || statistics.Count == 0) {
                throw new ArgumentNullException(nameof(statistics));
            }
            var data = new List<object>();
            foreach (var statistic in statistics) {
                var kv = new Dictionary<string, object> {
                    { "statisticName", statistic.Key },
                    { "statisticValue", statistic.Value },
                };
                data.Add(kv);
            }
            var path = string.Format("leaderboard/users/{0}/statistics", user.ObjectId);
            if (overwrite) {
                path = string.Format("{0}?overwrite=1", path);
            }
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Post,
                Content = data
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t => {
                try {
                    List<AVStatistic> statisticList = new List<AVStatistic>();
                    List<object> list = t.Result.Item2["results"] as List<object>;
                    foreach (object obj in list) {
                        statisticList.Add(AVStatistic.Parse(obj as IDictionary<string, object>));
                    }
                    return statisticList;
                } catch (Exception e) {
                    throw new AVException(AVException.ErrorCode.InvalidJSON, e.Message);
                }
            });
        }

        /// <summary>
        /// 获取用户成绩
        /// </summary>
        /// <returns>成绩列表</returns>
        /// <param name="user">用户</param>
        /// <param name="statisticNames">名称列表</param>
        public static Task<List<AVStatistic>> GetStatistics(AVUser user, List<string> statisticNames = null) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            var path = string.Format("leaderboard/users/{0}/statistics", user.ObjectId);
            if (statisticNames != null && statisticNames.Count > 0) {
                var names = string.Join(",", statisticNames.ToArray());
                path = string.Format("{0}?statistics={1}", path, names);
            }
            var sessionToken = AVUser.CurrentUser?.SessionToken;
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Post
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t => { 
                try {
                    List<AVStatistic> statisticList = new List<AVStatistic>();
                    List<object> list = t.Result.Item2["results"] as List<object>;
                    foreach (object obj in list) {
                        statisticList.Add(AVStatistic.Parse(obj as IDictionary<string, object>));
                    }
                    return statisticList;
                } catch (Exception e) {
                    throw new AVException(AVException.ErrorCode.InvalidJSON, e.Message);
                }
            });
        }

        /// <summary>
        /// 删除用户成绩
        /// </summary>
        /// <param name="user">用户</param>
        /// <param name="statisticNames">名称列表</param>
        public static Task DeleteStatistics(AVUser user, List<string> statisticNames) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (statisticNames == null || statisticNames.Count == 0) {
                throw new ArgumentNullException(nameof(statisticNames));
            }
            var path = string.Format("leaderboard/users/{0}/statistics", user.ObjectId);
            var names = string.Join(",", statisticNames.ToArray());
            path = string.Format("{0}?statistics={1}", path, names);
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Delete,
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command);
        }

        /// <summary>
        /// 获取排行榜历史数据
        /// </summary>
        /// <returns>排行榜归档列表</returns>
        /// <param name="skip">跳过数量</param>
        /// <param name="limit">分页数量</param>
        public Task<List<AVLeaderboardArchive>> GetArchives(int skip = 0, int limit = 10) {
            var path = string.Format("leaderboard/leaderboards/{0}/archives", StatisticName);
            path = string.Format("{0}?skip={1}&limit={2}", path, skip, limit);
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Get
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t => {
                List<AVLeaderboardArchive> archives = new List<AVLeaderboardArchive>();
                List<object> list = t.Result.Item2["results"] as List<object>;
                foreach (object obj in list) {
                    archives.Add(AVLeaderboardArchive.Parse(obj as IDictionary<string, object>));
                }
                return archives;
            });
        }

        /// <summary>
        /// 获取排行榜结果
        /// </summary>
        /// <returns>排名列表</returns>
        public Task<List<AVRanking>> GetResults(int version = -1, int skip = 0, int limit = 10, List<string> selectUserKeys = null, 
            List<string> includeStatistics = null) {
            return GetResults(null, version, skip, limit, selectUserKeys, includeStatistics);
        }

        /// <summary>
        /// 获取用户及附近的排名
        /// </summary>
        /// <returns>排名列表</returns>
        /// <param name="user">用户</param>
        /// <param name="version">版本号</param>
        /// <param name="skip">跳过数量</param>
        /// <param name="limit">分页数量</param>
        /// <param name="selectUserKeys">包含的玩家的字段列表</param>
        /// <param name="includeStatistics">包含的其他排行榜名称</param>
        public Task<List<AVRanking>> GetResultsAroundUser(int version = -1, int skip = 0, int limit = 10, 
            List<string> selectUserKeys = null,
            List<string> includeStatistics = null) {
            return GetResults(AVUser.CurrentUser, version, skip, limit, selectUserKeys, includeStatistics);
        }

        Task<List<AVRanking>> GetResults(AVUser user,
            int version, int skip, int limit,
            List<string> selectUserKeys,
            List<string> includeStatistics) {

            var path = string.Format("leaderboard/leaderboards/{0}/ranks", StatisticName);
            if (user != null) {
                path = string.Format("{0}/{1}", path, user.ObjectId);
            }
            path = string.Format("{0}?skip={1}&limit={2}", path, skip, limit);
            if (version != -1) {
                path = string.Format("{0}&version={1}", path, version);
            }
            if (selectUserKeys != null) {
                var keys = string.Join(",", selectUserKeys.ToArray());
                path = string.Format("{0}&includeUser={1}", path, keys);
            }
            if (includeStatistics != null) {
                var statistics = string.Join(",", includeStatistics.ToArray());
                path = string.Format("{0}&includeStatistics={1}", path, statistics);
            }
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Get
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t => {
                try {
                    List<AVRanking> rankingList = new List<AVRanking>();
                    List<object> list = t.Result.Item2["results"] as List<object>;
                    foreach (object obj in list) {
                        rankingList.Add(AVRanking.Parse(obj as IDictionary<string, object>));
                    }
                    return rankingList;
                } catch (Exception e) {
                    throw new AVException(AVException.ErrorCode.InvalidJSON, e.Message);
                }
            });
        }

        /// <summary>
        /// 设置更新策略
        /// </summary>
        /// <returns>排行榜对象</returns>
        /// <param name="updateStrategy">更新策略</param>
        public Task<AVLeaderboard> UpdateUpdateStrategy(AVLeaderboardUpdateStrategy updateStrategy) {
            var data = new Dictionary<string, object> {
                { "updateStrategy", updateStrategy.ToString().ToLower() }
            };
            return Update(data).OnSuccess(t => {
                UpdateStrategy = (AVLeaderboardUpdateStrategy)Enum.Parse(typeof(AVLeaderboardUpdateStrategy), t.Result["updateStrategy"].ToString().ToUpper());
                return this;
            });
        }

        /// <summary>
        /// 设置版本更新频率
        /// </summary>
        /// <returns>排行榜对象</returns>
        /// <param name="versionChangeInterval">版本更新频率</param>
        public Task<AVLeaderboard> UpdateVersionChangeInterval(AVLeaderboardVersionChangeInterval versionChangeInterval) {
            var data = new Dictionary<string, object> {
                { "versionChangeInterval", versionChangeInterval.ToString().ToLower() }
            };
            return Update(data).OnSuccess(t => {
                VersionChangeInterval = (AVLeaderboardVersionChangeInterval)Enum.Parse(typeof(AVLeaderboardVersionChangeInterval), t.Result["versionChangeInterval"].ToString().ToUpper());
                return this;
            });
        }

        Task<IDictionary<string,object>> Update(Dictionary<string, object> data) {
            var path = string.Format("leaderboard/leaderboards/{0}", StatisticName);
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Put,
                Content = data
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t => {
                return t.Result.Item2;
            });
        }

        /// <summary>
        /// 拉取排行榜数据
        /// </summary>
        /// <returns>排行榜对象</returns>
        public Task<AVLeaderboard> Fetch() {
            var path = string.Format("leaderboard/leaderboards/{0}", StatisticName);
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Get
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t => { 
                try {
                    // 反序列化 Leaderboard 对象
                    var leaderboard = Parse(t.Result.Item2);
                    return leaderboard;
                } catch (Exception e) {
                    throw new AVException(AVException.ErrorCode.InvalidJSON, e.Message);
                }
            });
        }

        /// <summary>
        /// 重置排行榜
        /// </summary>
        /// <returns>排行榜对象</returns>
        public Task<AVLeaderboard> Reset() {
            var path = string.Format("leaderboard/leaderboards/{0}/incrementVersion", StatisticName);
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Put
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t => { 
                try {
                    Init(t.Result.Item2);
                    return this;
                } catch (Exception e) { 
                    throw new AVException(AVException.ErrorCode.InvalidJSON, e.Message);
                }
            });
        }

        /// <summary>
        /// 销毁排行榜
        /// </summary>
        public Task Destroy() {
            var path = string.Format("leaderboard/leaderboards/{0}", StatisticName);
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Delete
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command);
        }

        static AVLeaderboard Parse(IDictionary<string, object> data) {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }
            var leaderboard = new AVLeaderboard();
            leaderboard.Init(data);
            return leaderboard;
        }

        void Init(IDictionary<string, object> data) {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }
            object nameObj;
            if (data.TryGetValue("statisticName", out nameObj)) {
                StatisticName = nameObj.ToString();
            }
            object orderObj;
            if (data.TryGetValue("order", out orderObj)) {
                Order = (AVLeaderboardOrder)Enum.Parse(typeof(AVLeaderboardOrder), orderObj.ToString().ToUpper());
            }
            object strategyObj;
            if (data.TryGetValue("updateStrategy", out strategyObj)) {
                UpdateStrategy = (AVLeaderboardUpdateStrategy)Enum.Parse(typeof(AVLeaderboardUpdateStrategy), strategyObj.ToString().ToUpper());
            }
            object intervalObj;
            if (data.TryGetValue("versionChangeInterval", out intervalObj)) {
                VersionChangeInterval = (AVLeaderboardVersionChangeInterval)Enum.Parse(typeof(AVLeaderboardVersionChangeInterval), intervalObj.ToString().ToUpper());
            }
            object versionObj;
            if (data.TryGetValue("version", out versionObj)) {
                Version = int.Parse(versionObj.ToString());
            }
        }
    }
}
