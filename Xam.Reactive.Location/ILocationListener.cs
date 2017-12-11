using System;

namespace Xam.Reactive.Location
{
    public partial interface ILocationListener
    {
        IObservable<bool> IsListeningForChanges { get; }
        IObservable<LocationRecorded> StartListeningForLocationChanges { get; }
    }
}