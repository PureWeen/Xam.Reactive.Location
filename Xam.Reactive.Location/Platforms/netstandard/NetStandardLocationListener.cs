using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks; 

namespace Xam.Reactive.Location
{
    public partial class LocationListener : ILocationListener
    {
        public LocationListener(
            ICheckPermissionProvider permissionProvider,
            IExceptionHandlerService exceptionHandling,
            ISchedulerFactory scheduler)
        {
        }

        public IObservable<LocationRecorded> PositionChanged() => throw new NotImplementedException("Use Platform project");

        public IObservable<Exception> OnError => throw new NotImplementedException("Use Platform project");

        IObservable<bool> ILocationListener.IsListeningForChanges => throw new NotImplementedException("Use Platform project");

        public IObservable<LocationRecorded> GetDeviceLocation(int timeoutMilliseconds)
        {
            throw new NotImplementedException("Use Platform project");
        }

        public IObservable<LocationRecorded> GetDeviceLocation(int timeoutMilliseconds, int howMany)
        {
            throw new NotImplementedException("Use Platform project");
        }

        public IObservable<LocationRecorded> GetLastKnownDeviceLocation(int ignoreIfOlderThanMilliseconds, int timeoutMilliseconds)
        {
            throw new NotImplementedException("Use Platform project");
        }
    }
}