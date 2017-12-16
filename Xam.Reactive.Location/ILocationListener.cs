using System;

namespace Xam.Reactive.Location
{
    public interface ILocationListener
    {
        IObservable<bool> IsListeningForChanges { get; }
        IObservable<LocationRecorded> PositionChanged();

        IObservable<Exception> OnError { get; }


        IObservable<LocationRecorded> GetLastKnownDeviceLocation(
            int ignoreIfOlderThanMilliseconds,
            int timeoutMilliseconds);


        IObservable<LocationRecorded> GetDeviceLocation(int timeoutMilliseconds);

        IObservable<LocationRecorded> GetDeviceLocation(int timeoutMilliseconds, int howMany);
    }
}