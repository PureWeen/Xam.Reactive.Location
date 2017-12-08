using System;

namespace Xam.Reactive
{
    public interface ILocationListener
    {
        bool IsListeningForChanges { get; }
        IObservable<LocationRecorded> WatchForPositionChanges { get; }
    }
}