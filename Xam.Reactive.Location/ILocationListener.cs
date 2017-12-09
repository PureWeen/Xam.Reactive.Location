using System;

namespace Xam.Reactive.Location
{
    public interface ILocationListener
    {
        IObservable<bool> IsListeningForChanges { get; }
        IObservable<LocationRecorded> StartListeningForLocationChanges { get; }
    }
}