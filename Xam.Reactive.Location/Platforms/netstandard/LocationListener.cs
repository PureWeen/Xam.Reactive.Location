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
using Xamarin.DispatchScheduler;

namespace Xam.Reactive
{
    public partial class LocationListener : ILocationListener
    {
        public LocationListener(
            ICheckPermissionProvider permissionProvider,
            IExceptionHandlerService exceptionHandling,
            ISchedulerFactory scheduler)
        {
        }

        public bool IsListeningForChanges => throw new NotImplementedException("Use Platform project");

        public IObservable<LocationRecorded> WatchForPositionChanges => throw new NotImplementedException("Use Platform project");
    }
}