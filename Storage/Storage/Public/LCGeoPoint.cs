using System;

namespace LeanCloud.Storage {
    /// <summary>
    /// LCGeoPoint represents a geographic location that may be associated
    /// with a key in a LCObject or used as a reference point for queries.
    /// </summary>
    public class LCGeoPoint {
        /// <summary>
        /// Gets the latitude of the LCGeoPoint.
        /// </summary>
        public double Latitude {
            get;
        }

        /// <summary>
        /// Gets the longitude of the LCGeoPoint.
        /// </summary>
        public double Longitude {
            get;
        }

        /// <summary>
        /// Constructs a LCGeoPoint with the specified latitude and longitude.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longtitude"></param>
        public LCGeoPoint(double latitude, double longtitude) {
            Latitude = latitude;
            Longitude = longtitude;
        }

        /// <summary>
        /// The original LCGeoPoint.
        /// </summary>
        public static LCGeoPoint Origin {
            get {
                return new LCGeoPoint(0, 0);
            }
        }

        /// <summary>
        /// Calculate the distance in kilometers between this point and another GeoPoint.
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
        /// Calculate the distance in miles between this point and another GeoPoint.
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
        /// Calculate the distance in radians between this point and another GeoPoint.
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
            return 2 * Math.Sin(Math.Sqrt(a));
        }
    }
}
