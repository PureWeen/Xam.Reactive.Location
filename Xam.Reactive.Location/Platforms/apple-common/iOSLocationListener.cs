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
    public partial class LocationListener : LocationListenerBase<CLLocation>
    {
        CLLocationManager _locationManager; 

        public LocationListener(
            ICheckPermissionProvider permissionProvider,
            IExceptionHandlerService exceptionHandling,
            ISchedulerFactory scheduler, 
            CLLocationManager locationManager = null) : base(permissionProvider, exceptionHandling, scheduler)
        { 
            _locationManager = locationManager;
            PositionFactory = createPositionFromPlatform;
        }


        protected override IObservable<LocationRecorded> CreateStartLocationUpdates()
        {
            return 
                Observable
                    .Create<LocationRecorded>(subj =>
                    {
                        CompositeDisposable disp = new CompositeDisposable();
                        try
                        {
                            IsListeningForChangesImperative = true;
                            if(_locationManager == null)
                            {
                                _locationManager = CreateLocationManager();
                                Observable
                                    .FromEventPattern<NSErrorEventArgs>
                                    (
                                        x => _locationManager.Failed += x,
                                        x => _locationManager.Failed -= x
                                    )
                                    .Select(e => e.EventArgs)
                                    .Subscribe(args =>
                                    {
                                        ExceptionHandling.LogException(
                                            new LocationActivationException(
                                                ActivationFailedReasons.CheckExceptionOnPlatform,
                                                args.Error.Description)
                                        );
                                    });
                            }
                            


                            Disposable.Create(() =>
                            {
                                _locationManager.StopUpdatingLocation();
                                IsListeningForChangesImperative = false;
                            })
                            .DisposeWith(disp);

                            _locationManager.StartUpdatingLocation();
                            Observable.FromEventPattern<CLLocationsUpdatedEventArgs>
                                (
                                    x => _locationManager.LocationsUpdated += x,
                                    x => _locationManager.LocationsUpdated -= x
                                )
                                .SelectMany(lu => lu.EventArgs.Locations)
                                .Select(lu => PositionFactory(lu))
                                .Where(lu => lu != null)
                                .Subscribe(subj)
                                .DisposeWith(disp);


                            if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
                            {
                                _locationManager.RequestLocation();
                            }

                            return disp;
                        }
                        catch (Exception exc)
                        {
                            subj.OnError(exc);
                            disp.Dispose();
                        }

                        return Disposable.Empty;
                    })
                    .SubscribeOn(Scheduler.Dispatcher);

        }

        LocationRecorded createPositionFromPlatform(CLLocation location)
        {
            try
            { 
                var p = new LocationRecorded();
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

        public static CLLocationManager CreateLocationManager()
        {
            // todo provide basic configuration options
            var returnValue = new CLLocationManager();
            returnValue.PausesLocationUpdatesAutomatically = false;
            returnValue.DesiredAccuracy = CLLocation.AccurracyBestForNavigation;
            returnValue.DistanceFilter = 10;
            returnValue.ActivityType = CLActivityType.AutomotiveNavigation;

            return returnValue;
        }



        public static LocationListener CreatePlatform
        (
            IExceptionHandlerService exceptionHandler = null,
            ISchedulerFactory scheduler = null,
            ICheckPermissionProvider permissionProvider = null,
            CLLocationManager locationManager = null
        )
        {
            scheduler = scheduler ?? CreateScheduler();
            exceptionHandler = exceptionHandler ?? CreateExceptionHandler();
            permissionProvider = permissionProvider ?? CreatePermissionProvider(scheduler);
            return new LocationListener(permissionProvider, exceptionHandler, scheduler, locationManager);
        }
    }
}