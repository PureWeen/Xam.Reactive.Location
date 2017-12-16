using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Xam.Reactive.Location
{
    public abstract class LocationListenerBase : ILocationListener
    {
        protected abstract IObservable<LocationRecorded> CreateStartLocationUpdates();


        static IObservable<LocationRecorded> NullPosition = Observable.Return<LocationRecorded>(null);
        const double DesiredAccuracy = 100;
        readonly ICheckPermissionProvider _permissionProvider;
        readonly IExceptionHandlerService _exceptionHandling;
        readonly ISchedulerFactory _scheduler;
        readonly BehaviorSubject<bool> _isListeningForChangesObs;
        Lazy<IObservable<LocationRecorded>> _PositionChanged;
        bool _isListeningForChangesImperative;
        protected LocationRecorded _lastPositionRecorded;


        public LocationListenerBase(
              ICheckPermissionProvider permissionProvider,
              IExceptionHandlerService exceptionHandling,
              ISchedulerFactory scheduler)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _exceptionHandling = exceptionHandling ?? throw new ArgumentNullException(nameof(exceptionHandling));
            _permissionProvider = permissionProvider ?? throw new ArgumentNullException(nameof(permissionProvider));

            _isListeningForChangesObs = new BehaviorSubject<bool>(false);
            _isListeningForChangesImperative = false;

            _PositionChanged =
               new Lazy<IObservable<LocationRecorded>>(createPositionChanged);
        }


        private IObservable<LocationRecorded> createPositionChanged()
        {
            return
                Observable.Defer(() =>
                    _permissionProvider
                        .CheckLocationPermission(_exceptionHandling)
                        .SelectMany(_ =>
                            CreateStartLocationUpdates()
                        )
                )
                .Catch((Exception exc) =>
                {
                    _exceptionHandling.LogException(exc);
                    return Observable.Throw<LocationRecorded>(exc);
                })
                .Repeat()
                .Log("PreRefCount:ActiveListener")
                .Publish()
                .RefCount()
                .Log("RefCount:ActiveListener");
        }

        protected bool IsListeningForChangesImperative
        {
            get
            {
                return _isListeningForChangesImperative;
            }
            set
            {
                if (_isListeningForChangesImperative != value)
                {
                    _isListeningForChangesImperative = value;
                    _isListeningForChangesObs.OnNext(value);
                }
            }
        }

        protected ICheckPermissionProvider PermissionProvider => _permissionProvider;
        protected ISchedulerFactory Scheduler => _scheduler;
        protected IExceptionHandlerService ExceptionHandling => _exceptionHandling;






        public IObservable<bool> IsListeningForChanges => 
            _isListeningForChangesObs.AsObservable().DistinctUntilChanged();

        public IObservable<LocationRecorded> PositionChanged() =>
            _PositionChanged.Value;

        public IObservable<Exception> OnError =>
            ExceptionHandling.OnError;

        public IObservable<LocationRecorded> GetLastKnownDeviceLocation(
            int ignoreIfOlderThanMilliseconds, 
            int timeoutMilliseconds)
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
                    .CatchAndLog(ExceptionHandling, localPositionRecorded)
                    .Take(1);
        }


        public IObservable<LocationRecorded> GetDeviceLocation(int timeoutMilliseconds)
        {
            return GetDeviceLocation(timeoutMilliseconds, 1);
        }

        public IObservable<LocationRecorded> GetDeviceLocation(int timeoutMilliseconds, int howMany)
        {
            return
                Observable.Defer(() => PositionChanged())
                    .CatchAndLog(ExceptionHandling, NullPosition)
                    .Timeout(TimeSpan.FromMilliseconds(timeoutMilliseconds), _scheduler.TaskPool)
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



    public abstract class LocationListenerBase<TPositionType> : LocationListenerBase
    {

        Func<TPositionType, LocationRecorded> _positionFactory;

        public LocationListenerBase(
            ICheckPermissionProvider permissionProvider, 
            IExceptionHandlerService exceptionHandling, 
            ISchedulerFactory scheduler) : base(permissionProvider, exceptionHandling, scheduler)
        {
        }


        public Func<TPositionType, LocationRecorded> PositionFactory
        {
            get => _positionFactory;
            set => _positionFactory = value ?? throw new ArgumentNullException($"{nameof(PositionFactory)}");
        }
    }

}
