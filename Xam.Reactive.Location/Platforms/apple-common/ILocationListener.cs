using CoreLocation;
using System;

namespace Xam.Reactive.Location
{
    public partial interface ILocationListener
    {
        IObservable<CLLocationManager> LocationManager { get; }
    }
}