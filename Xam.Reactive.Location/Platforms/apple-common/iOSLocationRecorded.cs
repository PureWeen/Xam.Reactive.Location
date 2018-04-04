using CoreLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xam.Reactive.Location
{
    public class iOSLocationRecorded : LocationRecorded
    {
        static DateTimeOffset baseAndroidTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
    

        public iOSLocationRecorded(CLLocation location)
        { 
            Accuracy = location.HorizontalAccuracy;
            Latitude = location.Coordinate.Latitude;
            Longitude = location.Coordinate.Longitude;

            if (location.VerticalAccuracy > -1)
            {
                Altitude = location.Altitude;
                AltitudeAccuracy = location.VerticalAccuracy;
            }

            if (location.Speed > -1)
                Speed = location.Speed;

            try
            {
                var date = (DateTime)location.Timestamp;
                Recorded = new DateTimeOffset(date);
            }
            catch (Exception)
            {
                Recorded = DateTimeOffset.UtcNow;
            }
        } 
    }
}
