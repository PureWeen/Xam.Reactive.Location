using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xam.Reactive.Location
{
    public class AndroidLocationRecorded : LocationRecorded
    {
        static DateTimeOffset baseAndroidTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public Android.Locations.Location Location { get; }

        public AndroidLocationRecorded(
            Android.Locations.Location location
            )
        {
            Location = location;
            Latitude = location.Latitude;
            Longitude = location.Longitude;
            Recorded = baseAndroidTime.AddMilliseconds(location.Time);
            Accuracy = location.Accuracy;
            Speed = location.Speed; 
        } 
    }
}
