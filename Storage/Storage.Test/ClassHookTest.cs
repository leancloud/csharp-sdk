using NUnit.Framework;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    public class ClassHookTest : BaseTest {
        private const string CLASS_NAME = "TestHookClass";

        private LCObject obj;

        [Test]
        [Order(0)]
        public async Task Save() {
            obj = new LCObject(CLASS_NAME);
            await obj.Save();
            await obj.Fetch();
            Assert.AreEqual(obj["score"], 60);
        }

        [Test]
        [Order(10)]
        public async Task Update() {
            obj["score"] = 200;
            LCException e = Assert.ThrowsAsync<LCException>(() => obj.Save());
            Assert.AreEqual(e.Code, 142);

            obj["score"] = 90;
            await obj.Save();
        }

        [Test]
        [Order(20)]
        public void Delete() {
            LCException e = Assert.ThrowsAsync<LCException>(() => obj.Delete());
            Assert.AreEqual(e.Code, 142);
        }
    }
}
