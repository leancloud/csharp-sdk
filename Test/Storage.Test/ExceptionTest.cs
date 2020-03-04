using NUnit.Framework;
using LeanCloud.Storage;
using LeanCloud.Common;

namespace LeanCloud.Test {
    public class ExceptionTest {
        [Test]
        public void LeanCloudException() {
            try {
                throw new LCException(123, "hello, world");
            } catch (LCException e) {
                TestContext.WriteLine($"{e.Code} : {e.Message}");
                Assert.AreEqual(e.Code, 123);
            }
        }
    }
}
