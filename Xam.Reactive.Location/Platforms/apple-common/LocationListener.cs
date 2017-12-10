using CoreLocation;
using Foundation;
using System; 
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects; 
using UIKit; 

namespace Xam.Reactive.Location
{
    public partial class LocationListener : ILocationListener
    {
        CLLocationManager _locationManager;
        readonly Lazy<IObservable<LocationRecorded>> _startListeningForLocationChanges;
        CLLocationManager Manager => _locationManager;

        public IObservable<LocationRecorded> StartListeningForLocationChanges => 
            _startListeningForLocationChanges.Value;

        readonly BehaviorSubject<bool> _isListeningForChangesObs;
        bool _isListeningForChangesImperative;

        readonly ICheckPermissionProvider _permissionProvider;
        readonly IExceptionHandlerService _exceptionHandling;
        readonly ISchedulerFactory _scheduler;

        public LocationListener(
            ICheckPermissionProvider permissionProvider,
            IExceptionHandlerService exceptionHandling,
            ISchedulerFactory scheduler)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _exceptionHandling = exceptionHandling ?? throw new ArgumentNullException(nameof(exceptionHandling));
            _permissionProvider = permissionProvider ?? throw new ArgumentNullException(nameof(permissionProvider));

            _isListeningForChangesObs = new BehaviorSubject<bool>(false);

            _scheduler
                .Dispatcher
                .Schedule(() =>
                {
                    _locationManager = OnCreateLocationManager();

                    if(_locationManager == null)
                    {
                        _locationManager = new CLLocationManager();
                        _locationManager.PausesLocationUpdatesAutomatically = false;
                        _locationManager.DesiredAccuracy = CLLocation.AccurracyBestForNavigation;
                        _locationManager.DistanceFilter = 10;
                        _locationManager.ActivityType = CLActivityType.AutomotiveNavigation;
                    }

                    Observable.FromEventPattern<NSErrorEventArgs>
                        (
                            x => _locationManager.Failed += x,
                            x => _locationManager.Failed -= x
                        )
                        .Select(e => e.EventArgs)
                        .Subscribe(args =>
                        {
                            _exceptionHandling.LogException(
                                new LocationActivationException(
                                    ActivationFailedReasons.CheckExceptionOnPlatform, 
                                    args.Error.Description)
                            );
                        });
                });



            _startListeningForLocationChanges =
                new Lazy<IObservable<LocationRecorded>>(createStartListeningForLocationChanges); 
        }

        private IObservable<LocationRecorded> createStartLocationUpdates()
        {
            return 
                Observable
                    .Create<LocationRecorded>(subj =>
                    {

                        CompositeDisposable disp = new CompositeDisposable();

                        try
                        {
                            Manager.StartUpdatingLocation();
                            IsListeningForChangesImperative = true;
                            Observable.FromEventPattern<CLLocationsUpdatedEventArgs>
                                (
                                    x => Manager.LocationsUpdated += x,
                                    x => Manager.LocationsUpdated -= x
                                )
                                .SelectMany(lu => lu.EventArgs.Locations)
                                .Select(lu => createPositionFromPlatform(lu))
                                .Where(lu => lu != null)
                                .Subscribe(subj)
                                .DisposeWith(disp);


                            if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
                            {
                                Manager.RequestLocation();
                            }

                            return Disposable.Create(() =>
                            {
                                disp.Dispose();
                                Manager.StopUpdatingLocation();
                                IsListeningForChangesImperative = false;
                            });
                        }
                        catch (Exception exc)
                        {
                            subj.OnError(exc);
                            disp.Dispose();
                        }

                        return Disposable.Empty;
                    })
                    .SubscribeOn(_scheduler.Dispatcher);

        }

        private IObservable<LocationRecorded> createStartListeningForLocationChanges()
        {
            return
                Observable.Defer(() =>
                    _permissionProvider
                        .CheckLocationPermission()
                        .SelectMany(_=> createStartLocationUpdates())
                )
                .Catch((Exception exc) =>
                {
                    _exceptionHandling.LogException(exc);
                    return Observable.Empty<LocationRecorded>();
                })
                .Log("PreRefCount:ActiveListener")
                .Publish()
                .RefCount()
                .Log("RefCount:ActiveListener");
        }

        public IObservable<bool> IsListeningForChanges => 
            _isListeningForChangesObs
                .AsObservable()
                .DistinctUntilChanged();

        bool IsListeningForChangesImperative
        {
            get
            {
                return _isListeningForChangesImperative;
            }
            set
            {
                if(_isListeningForChangesImperative != value)
                {
                    _isListeningForChangesImperative = value;
                    _isListeningForChangesObs.OnNext(value);
                }
            }
        }



        public virtual CLLocationManager OnCreateLocationManager()
        {
            return null;
        }

        LocationRecorded createPositionFromPlatform(CLLocation location)
        {
            try
            {
                var p = OnCreatePositionFromPlatform(location);
                if(p != null)
                {
                    return p;
                }

                p = new LocationRecorded();
                p.Accuracy = location.HorizontalAccuracy;
                p.Latitude = location.Coordinate.Latitude;
                p.Longitude = location.Coordinate.Longitude;

                if (location.VerticalAccuracy > -1)
                {
                    p.Altitude = location.Altitude;
                    p.AltitudeAccuracy = location.VerticalAccuracy;
                }

                if (location.Speed > -1)
                    p.Speed = location.Speed;

                try
                {
                    var date = (DateTime)location.Timestamp;
                    p.Recorded = new DateTimeOffset(date);
                }
                catch (Exception)
                {
                    p.Recorded = DateTimeOffset.UtcNow;
                }

                return p;

            }
            finally
            {
                location.Dispose();
            }
        }

        public virtual LocationRecorded OnCreatePositionFromPlatform(CLLocation location)
        {
            return null;
        }
    }
}