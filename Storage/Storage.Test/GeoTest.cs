using NUnit.Framework;
using System;
using System.Threading.Tasks;
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

        [Test]
        public async Task Query() {
            LCObject geoObj = new LCObject("GeoObj");
            Random random = new Random();
            LCGeoPoint p1 = new LCGeoPoint(-90 + random.NextDouble() * 180, -180 + random.NextDouble() * 360);
            geoObj["location"] = p1;
            await geoObj.Save();

            LCGeoPoint p2 = new LCGeoPoint(p1.Latitude + 0.01, p1.Longitude + 0.01);

            double km = p1.KilometersTo(p2);
            TestContext.WriteLine($"km: {km}, {Math.Ceiling(km)}");
            LCQuery<LCObject> query = new LCQuery<LCObject>("GeoObj");
            query.WhereWithinKilometers("location", p2, Math.Ceiling(km));
            Assert.Greater((await query.Find()).Count, 0);

            double miles = p1.MilesTo(p2);
            query = new LCQuery<LCObject>("GeoObj");
            query.WhereWithinMiles("location", p2, Math.Ceiling(miles));
            Assert.Greater((await query.Find()).Count, 0);

            double radians = p1.RadiansTo(p2);
            query = new LCQuery<LCObject>("GeoObj");
            query.WhereWithinRadians("location", p2, Math.Ceiling(radians));
            Assert.Greater((await query.Find()).Count, 0);
        }
    }
}
