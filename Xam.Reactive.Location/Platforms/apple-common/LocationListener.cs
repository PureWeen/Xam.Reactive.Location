using CoreLocation;
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
using UIKit;
using Xamarin.DispatchScheduler;

namespace Xam.Reactive
{
    public partial class LocationListener : ILocationListener
    {
        CLLocationManager _locationManager;
        readonly Lazy<IObservable<LocationRecorded>> _watchForPositionChanges;

        CLLocationManager Manager => _locationManager;

        public bool IsListeningForChanges { get; private set; }

        public IObservable<LocationRecorded> WatchForPositionChanges => _watchForPositionChanges.Value;

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

            _scheduler
                .Dispatcher
                .Schedule(() =>
                {
                    _locationManager = OnCreateLocationManager();

                    if(_locationManager == null)
                    {
                        _locationManager.PausesLocationUpdatesAutomatically = false;
                        _locationManager.DesiredAccuracy = CLLocation.AccurracyBestForNavigation;
                        _locationManager.DistanceFilter = 10;
                        _locationManager.ActivityType = CLActivityType.AutomotiveNavigation;
                    }
                });

            _watchForPositionChanges =
                new Lazy<IObservable<LocationRecorded>>(() =>
                {                    
                    var checkPermission =
                        Observable.Defer(() => 
                            _permissionProvider.Location
                                .Where(hasPermission => hasPermission)
                        );

                    var startLocationUpdates =
                        Observable.Create<LocationRecorded>(subj =>
                        {
                            Manager.StartUpdatingLocation();
                            IsListeningForChanges = true;
                            var disp = 
                                Observable.FromEventPattern<EventHandler<CLLocationsUpdatedEventArgs>, CLLocationsUpdatedEventArgs>(
                                    x=> Manager.LocationsUpdated += x,
                                    x => Manager.LocationsUpdated -=x
                                    )
                                    .SelectMany(lu => lu.EventArgs.Locations)
                                    .Select(lu => createPositionFromPlatform(lu))
                                    .Where(lu => lu != null)
                                    .Subscribe(subj);

                            if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
                            {
                                Manager.RequestLocation();
                            }

                            return Disposable.Create(() =>
                            {
                                disp.Dispose();
                                Manager.StopUpdatingLocation();
                                IsListeningForChanges = false;
                            });
                        })
                        .SubscribeOn(_scheduler.Dispatcher);

                    return
                        checkPermission
                            .SelectMany(_ => startLocationUpdates)
                            .Publish()
                            .RefCount();
                }
            );
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