using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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
        }

        [Test]
        [Order(10)]
        public async Task Update() {
            obj["score"] = 200;
            LCException e = Assert.CatchAsync<LCException>(() => obj.Save());
            Assert.AreEqual(e.Code, 142);

            obj["score"] = 90;
            await obj.Save();
        }

        [Test]
        [Order(20)]
        public void Delete() {
            LCException e = Assert.CatchAsync<LCException>(() => obj.Delete());
            Assert.AreEqual(e.Code, 142);
        }
    }
}
