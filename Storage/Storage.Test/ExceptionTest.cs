using NUnit.Framework;
using LeanCloud;

namespace Storage.Test {
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
