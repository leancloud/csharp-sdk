using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
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
                int score = i * 10;
                user["score"] = score;
                await user.Save();
                await LCLeaderboard.UpdateStatistics(user, new Dictionary<string, double> {
                    { leaderboardName, score }
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
            LCLeaderboard leaderboard = await LCLeaderboard.GetLeaderboard(leaderboardName);
            ReadOnlyCollection<LCRanking> rankings = await leaderboard.GetResults(selectKeys: new string[] { "score" });
            foreach (LCRanking ranking in rankings) {
                WriteLine($"{ranking.Rank} : {ranking.User.ObjectId}, {ranking.Value}");
                Assert.NotNull(ranking.User["score"]);
            }
        }

        [Test]
        [Order(4)]
        public async Task GetResultsOfMe() {
            int today = DateTimeOffset.Now.DayOfYear;
            string username = $"{today}_0";
            string password = "leancloud";
            await LCUser.Login(username, password);
            LCLeaderboard leaderboard = await LCLeaderboard.GetLeaderboard(leaderboardName);
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

    public class ObjectLeaderboardTest : BaseTest {
        private const string LeaderboaderObjectClassName = "LeaderboaderObject";

        private string leaderboardName;

        private Dictionary<string, LCObject> objDict = new Dictionary<string, LCObject>();

        [SetUp]
        public override void SetUp() {
            base.SetUp();
            LCApplication.Initialize(AppId, AppKey, AppServer, MasterKey);
            LCApplication.UseMasterKey = true;
            leaderboardName = $"Leaderboard_Object_{DateTimeOffset.Now.DayOfYear}";
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
                LCLeaderboard oldLeaderboard = await LCLeaderboard.GetLeaderboard(leaderboardName);
                await oldLeaderboard.Destroy();
            } catch (Exception e) {
                WriteLine(e.Message);
            }
            LCLeaderboard leaderboard = await LCLeaderboard.CreateLeaderboard(leaderboardName, memberType: LeaderboaderObjectClassName);
            Assert.AreEqual(leaderboard.StatisticName, leaderboardName);
        }

        [Test]
        [Order(1)]
        public async Task Update() {
            for (int i = 0; i < 10; i++) {
                int today = DateTimeOffset.Now.DayOfYear;
                LCObject obj = LCObject.Create(LeaderboaderObjectClassName);
                obj["name"] = $"{today}_{i}";
                await obj.Save();
                objDict[obj.ObjectId] = obj;
                await LCLeaderboard.UpdateStatistics(obj, new Dictionary<string, double> {
                    { leaderboardName, i * 10 }
                });
            }
        }

        [Test]
        [Order(2)]
        public async Task GetStatistics() {
            LCQuery<LCObject> query = new LCQuery<LCObject>(LeaderboaderObjectClassName);
            Dictionary<string, LCObject>.KeyCollection.Enumerator enumerator = objDict.Keys.GetEnumerator();
            enumerator.MoveNext();
            LCObject obj = await query.Get(enumerator.Current);
            ReadOnlyCollection<LCStatistic> statistics = await LCLeaderboard.GetStatistics(obj);
            foreach (LCStatistic statistic in statistics) {
                WriteLine($"{statistic.Name} : {statistic.Value}");
            }
        }

        [Test]
        [Order(3)]
        public async Task GetResults() {
            LCLeaderboard leaderboard = await LCLeaderboard.GetLeaderboard(leaderboardName);
            ReadOnlyCollection<LCRanking> rankings = await leaderboard.GetResults(selectKeys: new string[] { "name" });
            foreach (LCRanking ranking in rankings) {
                WriteLine($"{ranking.Rank} : {ranking.Object.ObjectId}, {ranking.Value}");
                Assert.NotNull(ranking.Object["name"]);
            }
        }

        [Test]
        [Order(4)]
        public async Task GetResultsOfObject() {
            LCQuery<LCObject> query = new LCQuery<LCObject>(LeaderboaderObjectClassName);
            Dictionary<string, LCObject>.KeyCollection.Enumerator enumerator = objDict.Keys.GetEnumerator();
            enumerator.MoveNext();
            LCObject obj = await query.Get(enumerator.Current);
            LCLeaderboard leaderboard = await LCLeaderboard.GetLeaderboard(leaderboardName);
            ReadOnlyCollection<LCRanking> rankings = await leaderboard.GetResults(obj);
            foreach (LCRanking ranking in rankings) {
                WriteLine($"{ranking.Rank} : {ranking.Object.ObjectId}, {ranking.Value}");
            }
            Assert.NotNull(rankings.First(ranking => ranking.Object.ObjectId == obj.ObjectId));
        }

        [Test]
        [Order(5)]
        public async Task GetOtherStatistics() {
            LCQuery<LCObject> query = new LCQuery<LCObject>(LeaderboaderObjectClassName);
            Dictionary<string, LCObject>.KeyCollection.Enumerator enumerator = objDict.Keys.GetEnumerator();
            enumerator.MoveNext();
            LCObject obj = await query.Get(enumerator.Current);
            ReadOnlyCollection<LCStatistic> statistics = await LCLeaderboard.GetStatistics(obj);
            foreach (LCStatistic statistic in statistics) {
                WriteLine($"{statistic.Name}, {statistic.Value}");
            }
        }
    }

    public class EntityLeaderboardTest : BaseTest {
        private string leaderboardName;

        [SetUp]
        public override void SetUp() {
            base.SetUp();
            LCApplication.Initialize(AppId, AppKey, AppServer, MasterKey);
            LCApplication.UseMasterKey = true;
            leaderboardName = $"Leaderboard_Entity_{DateTimeOffset.Now.DayOfYear}";
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
                LCLeaderboard oldLeaderboard = await LCLeaderboard.GetLeaderboard(leaderboardName);
                await oldLeaderboard.Destroy();
            } catch (Exception e) {
                WriteLine(e.Message);
            }
            LCLeaderboard leaderboard = await LCLeaderboard.CreateLeaderboard(leaderboardName, memberType: LCLeaderboard.ENTITY_MEMBER_TYPE);
            Assert.AreEqual(leaderboard.StatisticName, leaderboardName);
        }

        [Test]
        [Order(1)]
        public async Task Update() {
            for (int i = 0; i < 10; i++) {
                int today = DateTimeOffset.Now.DayOfYear;
                string entity = $"{today}_{i}";
                await LCLeaderboard.UpdateStatistics(entity, new Dictionary<string, double> {
                    { leaderboardName, i * 10 }
                });
            }
        }

        [Test]
        [Order(2)]
        public async Task GetStatistics() {
            int today = DateTimeOffset.Now.DayOfYear;
            string entity = $"{today}_0";
            ReadOnlyCollection<LCStatistic> statistics = await LCLeaderboard.GetStatistics(entity);
            foreach (LCStatistic statistic in statistics) {
                WriteLine($"{statistic.Name} : {statistic.Value}");
            }
        }

        [Test]
        [Order(3)]
        public async Task GetResults() {
            LCLeaderboard leaderboard = await LCLeaderboard.GetLeaderboard(leaderboardName);
            ReadOnlyCollection<LCRanking> rankings = await leaderboard.GetResults();
            foreach (LCRanking ranking in rankings) {
                WriteLine($"{ranking.Rank} : {ranking.Entity}, {ranking.Value}");
            }
        }

        [Test]
        [Order(4)]
        public async Task GetResultsOfEntity() {
            int today = DateTimeOffset.Now.DayOfYear;
            string entity = $"{today}_0";
            LCLeaderboard leaderboard = await LCLeaderboard.GetLeaderboard(leaderboardName);
            ReadOnlyCollection<LCRanking> rankings = await leaderboard.GetResults(entity);
            foreach (LCRanking ranking in rankings) {
                WriteLine($"{ranking.Rank} : {ranking.Entity}, {ranking.Value}");
            }
            Assert.NotNull(rankings.First(ranking => ranking.Entity == entity));
        }

        [Test]
        [Order(5)]
        public async Task GetOtherStatistics() {
            int today = DateTimeOffset.Now.DayOfYear;
            string entity = $"{today}_0";
            ReadOnlyCollection<LCStatistic> statistics = await LCLeaderboard.GetStatistics(entity);
            foreach (LCStatistic statistic in statistics) {
                WriteLine($"{statistic.Name}, {statistic.Value}");
            }
        }
    }
}
