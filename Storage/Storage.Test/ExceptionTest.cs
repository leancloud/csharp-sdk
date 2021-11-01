using NUnit.Framework;
using LeanCloud;

namespace Storage.Test {
    public class ExceptionTest : BaseTest {
        [Test]
        public void LeanCloudException() {
            LCException e = Assert.Catch<LCException>(() => throw new LCException(123, "hello, exception"));
            TestContext.WriteLine($"{e.Code} : {e.Message}");
            Assert.AreEqual(e.Code, 123);
        }
    }
}
