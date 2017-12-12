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

        public IObservable<LocationRecorded> StartListeningForLocationChanges => throw new NotImplementedException("Use Platform project");

        IObservable<bool> ILocationListener.IsListeningForChanges => throw new NotImplementedException("Use Platform project");
    }
}