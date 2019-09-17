using NUnit.Framework;
using LeanCloud;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace LeanCloud.Test {
    public class RelationTest {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina();
        }

        [Test]
        public async Task CreateRelation() {
            AVObject tag1 = new AVObject("Tag") {
                ["name"] = "hello"
            };
            await tag1.SaveAsync();
            AVObject tag2 = new AVObject("Tag") {
                { "name", "world" }
            };
            await tag2.SaveAsync();
            AVObject todo = new AVObject("Todo");
            AVRelation<AVObject> relation = todo.GetRelation<AVObject>("tags");
            relation.Add(tag1);
            relation.Add(tag2);
            await todo.SaveAsync();
        }

        [Test]
        public async Task QueryRelation() {
            AVQuery<AVObject> query = new AVQuery<AVObject>("Todo");
            query.OrderByDescending("createdAt");
            AVObject todo = await query.FirstAsync();
            AVRelation<AVObject> relation = todo.GetRelation<AVObject>("tags");
            AVQuery<AVObject> tagQuery = relation.Query;
            IEnumerable<AVObject> tags = await tagQuery.FindAsync();
            Assert.Greater(tags.Count(), 0);
            TestContext.Out.WriteLine($"count: {tags.Count()}");
            foreach (AVObject tag in tags) {
                TestContext.Out.WriteLine($"{tag.ObjectId}, {tag["name"]}");
            }
        }
    }
}
