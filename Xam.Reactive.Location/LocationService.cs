using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.DispatchScheduler;

namespace Xam.Reactive
{
    public partial class LocationService
    {
        IObservable<LocationRecorded> NullPosition =
            Observable.Return<LocationRecorded>(null);

        public const double DesiredAccuracy = 100; 
        protected LocationRecorded _lastPositionRecorded;

        public ILocationListener Listener { get; }
        public IExceptionHandlerService ExceptionService { get; }

        readonly ISchedulerFactory _scheduler;

        public LocationService() 
        {
            ExceptionService = new ExceptionHandlerService();
            _scheduler = new XamarinSchedulerFactory();
            Listener = new LocationListener(new NullCheckPermissionProvider(), ExceptionService, _scheduler);
        }

        public LocationService(
            ILocationListener listener, 
            IExceptionHandlerService exceptionHandler,
            ISchedulerFactory scheduler)
        {

            Listener = listener ?? throw new ArgumentNullException(nameof(listener));
            ExceptionService = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }
 

        public bool IsListeningForChanges =>
            Listener.IsListeningForChanges;


        public IObservable<LocationRecorded> WatchForPositionChange =>
            Listener.WatchForPositionChanges;



        public IObservable<LocationRecorded> GetLastKnownDeviceLocation(
            int ignoreIfOlderThanMilliseconds, int timeoutMilliseconds)
        {
            if (timeoutMilliseconds <= 0) throw new ArgumentException($"{nameof(timeoutMilliseconds)} cannot be less than zero");
            if (ignoreIfOlderThanMilliseconds <= 0) throw new ArgumentException($"{nameof(ignoreIfOlderThanMilliseconds)} cannot be less than zero");

            var localPositionRecorded = _lastPositionRecorded;
            IObservable<LocationRecorded> returnValue = Observable.Return(localPositionRecorded);


            Func<LocationRecorded, bool> validPosition = (position) =>
            {
                if (position == null) return false; 
                var age = DateTimeOffset.UtcNow.Subtract(position.Recorded).TotalMilliseconds;
                return position != null && age < ignoreIfOlderThanMilliseconds;
            };

            // just try real quick if it's possible
            if (validPosition(localPositionRecorded))
            {
                return Observable.Return(localPositionRecorded);
            }

            return
                GetDeviceLocation(timeoutMilliseconds, Int32.MaxValue)
                    .Where(position => validPosition(position))
                    .CatchAndLog(ExceptionService, localPositionRecorded)
                    .Take(1);
        }
         
 
        public IObservable<LocationRecorded> GetDeviceLocation(int timeoutMilliseconds)
        {
            return GetDeviceLocation(timeoutMilliseconds, 1);
        }

        public IObservable<LocationRecorded> GetDeviceLocation(int timeoutMilliseconds, int howMany)
        {
            var timeoutObs =
                Observable.Timer(TimeSpan.FromMilliseconds(timeoutMilliseconds), scheduler: _scheduler.TaskPool)
                    .SelectMany(_ => Observable.Throw<LocationRecorded>(new TimeoutException()));

            return
                Observable.Defer(() => WatchForPositionChange)
                    .CatchAndLog(ExceptionService, NullPosition)
                    .Amb(timeoutObs)
                    .Take(howMany)
                    .Select(position =>
                    {
                        if (position == null)
                            return null;

                        _lastPositionRecorded = position;
                        return _lastPositionRecorded;
                    });
        }

    }
}
