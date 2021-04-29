using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Storage;

using static NUnit.Framework.TestContext;

namespace Storage.Test {
    public class LeaderboardTest : BaseTest {
        private string leaderboardName;

        [SetUp]
        public override void SetUp() {
            base.SetUp();
            LCApplication.Initialize(AppId, AppKey, AppServer, MasterKey);
            LCApplication.UseMasterKey = true;
            leaderboardName = $"Leaderboard_{DateTimeOffset.Now.DayOfYear}";
        }

        [TearDown]
        public override void TearDown() {
            base.TearDown();
            LCApplication.UseMasterKey = false;
        }

        [Test]
        [Order(0)]
        public async Task Create() {
            try {
                LCLeaderboard oldLeaderboard = LCLeaderboard.CreateWithoutData(leaderboardName);
                await oldLeaderboard.Destroy();
            } catch (Exception e) {
                WriteLine(e.Message);
            }
            LCLeaderboard leaderboard = await LCLeaderboard.CreateLeaderboard(leaderboardName);
            Assert.AreEqual(leaderboard.StatisticName, leaderboardName);
        }

        [Test]
        [Order(1)]
        public async Task Update() {
            for (int i = 0; i < 10; i++) {
                int today = DateTimeOffset.Now.DayOfYear;
                string username = $"{today}_{i}";
                string password = "leancloud";
                LCUser user;
                try {
                    user = await LCUser.Login(username, password);
                } catch (Exception) {
                    user = new LCUser {
                        Username = username,
                        Password = password
                    };
                    await user.SignUp();
                }
                await LCLeaderboard.UpdateStatistics(user, new Dictionary<string, double> {
                    { leaderboardName, i * 10 }
                });
            }
        }

        [Test]
        [Order(2)]
        public async Task GetStatistics() {
            int today = DateTimeOffset.Now.DayOfYear;
            string username = $"{today}_0";
            string password = "leancloud";
            LCUser user = await LCUser.Login(username, password);
            ReadOnlyCollection<LCStatistic> statistics = await LCLeaderboard.GetStatistics(user);
            foreach (LCStatistic statistic in statistics) {
                WriteLine($"{statistic.Name} : {statistic.Value}");
            }
        }

        [Test]
        [Order(3)]
        public async Task GetResults() {
            LCLeaderboard leaderboard = LCLeaderboard.CreateWithoutData(leaderboardName);
            ReadOnlyCollection<LCRanking> rankings = await leaderboard.GetResults();
            foreach (LCRanking ranking in rankings) {
                WriteLine($"{ranking.Rank} : {ranking.User.ObjectId}, {ranking.Value}");
            }
        }

        [Test]
        [Order(4)]
        public async Task GetResultsOfMe() {
            int today = DateTimeOffset.Now.DayOfYear;
            string username = $"{today}_0";
            string password = "leancloud";
            await LCUser.Login(username, password);
            LCLeaderboard leaderboard = LCLeaderboard.CreateWithoutData(leaderboardName);
            ReadOnlyCollection<LCRanking> rankings = await leaderboard.GetResultsAroundUser(limit: 5);
            foreach (LCRanking ranking in rankings) {
                WriteLine($"{ranking.Rank} : {ranking.User.ObjectId}, {ranking.Value}");
            }
        }

        [Test]
        [Order(5)]
        public async Task GetOtherStatistics() {
            int today = DateTimeOffset.Now.DayOfYear;
            string username = $"{today}_0";
            string password = "leancloud";
            LCUser user = await LCUser.Login(username, password);
            await LCUser.Login($"{today}_1", password);
            ReadOnlyCollection<LCStatistic> statistics = await LCLeaderboard.GetStatistics(user);
            foreach (LCStatistic statistic in statistics) {
                WriteLine($"{statistic.Name}, {statistic.Value}");
            }
        }
    }
}
