using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using LeanCloud.Storage;

namespace Storage.Test {
    public class RelationTest : BaseTest {
        private LCObject parent;
        private LCObject c1;
        private LCObject c2;

        [Test]
        [Order(0)]
        public async Task AddAndRemove() {
            parent = new LCObject("Parent");
            c1 = new LCObject("Child");
            parent.AddRelation("children", c1);
            c2 = new LCObject("Child");
            parent.AddRelation("children", c2);
            await parent.Save();

            LCRelation<LCObject> relation = parent["children"] as LCRelation<LCObject>;
            LCQuery<LCObject> query = relation.Query;
            int count = await query.Count();

            TestContext.WriteLine($"count: {count}");
            Assert.AreEqual(count, 2);

            parent.RemoveRelation("children", c2);
            await parent.Save();

            int count2 = await query.Count();
            TestContext.WriteLine($"count: {count2}");
            Assert.AreEqual(count2, 1);
        }

        [Test]
        [Order(1)]
        public async Task Query() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Parent");
            LCObject queryParent = await query.Get(parent.ObjectId);
            LCRelation<LCObject> relation = queryParent["children"] as LCRelation<LCObject>;

            TestContext.WriteLine(relation.Key);
            TestContext.WriteLine(relation.Parent);
            TestContext.WriteLine(relation.TargetClass);

            Assert.NotNull(relation.Key);
            Assert.NotNull(relation.Parent);
            Assert.NotNull(relation.TargetClass);

            LCQuery<LCObject> relationQuery = relation.Query;
            ReadOnlyCollection<LCObject> results = await relationQuery.Find();
            foreach (LCObject item in results) {
                TestContext.WriteLine(item.ObjectId);
                Assert.NotNull(item.ObjectId);
            }
        }
    }
}
