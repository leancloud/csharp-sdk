using NUnit.Framework;
using LeanCloud.Storage;

namespace Storage.Test {
    public class GeoTest : BaseTest {
        [Test]
        public void Calculate() {
            LCGeoPoint p1 = new LCGeoPoint(20.0059, 110.3665);
            LCGeoPoint p2 = new LCGeoPoint(20.0353, 110.3645);
            double kilometers = p1.KilometersTo(p2);
            TestContext.WriteLine(kilometers);
            Assert.Less(kilometers - 3.275, 0.01);

            double miles = p1.MilesTo(p2);
            TestContext.WriteLine(miles);
            Assert.Less(miles - 2.035, 0.01);

            double radians = p1.RadiansTo(p2);
            TestContext.WriteLine(radians);
            Assert.Less(radians - 0.0005, 0.0001);
        }
    }
}
