using System;

namespace LeanCloud.Storage {
    public class LCGeoPoint {
        /// <summary>
        /// 纬度
        /// </summary>
        public double Latitude {
            get;
        }

        /// <summary>
        /// 经度
        /// </summary>
        public double Longitude {
            get;
        }

        public LCGeoPoint(double latitude, double longtitude) {
            Latitude = latitude;
            Longitude = longtitude;
        }

        public static LCGeoPoint Origin {
            get {
                return new LCGeoPoint(0, 0);
            }
        }

        /// <summary>
        /// 据某点的距离（单位：千米）
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public double KilometersTo(LCGeoPoint point) {
            if (point == null) {
                throw new ArgumentNullException(nameof(point));
            }
            return RadiansTo(point) * 6371.0;
        }

        /// <summary>
        /// 据某点的距离（单位：英里）
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public double MilesTo(LCGeoPoint point) {
            if (point == null) {
                throw new ArgumentNullException(nameof(point));
            }
            return RadiansTo(point) * 3958.8;
        }

        /// <summary>
        /// 据某点的距离（单位：弧度）
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public double RadiansTo(LCGeoPoint point) {
            if (point == null) {
                throw new ArgumentNullException(nameof(point));
            }
            double d2r = Math.PI / 180.0;
            double lat1rad = Latitude * d2r;
            double long1rad = Longitude * d2r;
            double lat2rad = point.Latitude * d2r;
            double long2rad = point.Longitude * d2r;
            double deltaLat = lat1rad - lat2rad;
            double deltaLong = long1rad - long2rad;
            double sinDeltaLatDiv2 = Math.Sin(deltaLat / 2);
            double sinDeltaLongDiv2 = Math.Sin(deltaLong / 2);
            double a = sinDeltaLatDiv2 * sinDeltaLatDiv2 +
                Math.Cos(lat1rad) * Math.Cos(lat2rad) * sinDeltaLongDiv2 * sinDeltaLongDiv2;
            a = Math.Min(1.0, a);
            return 2 * Math.Cos(Math.Sqrt(a));
        }
    }
}
