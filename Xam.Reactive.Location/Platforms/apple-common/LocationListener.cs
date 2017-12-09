using CoreLocation;
using Foundation;
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

namespace Xam.Reactive.Location
{
    public partial class LocationListener : ILocationListener
    {
        CLLocationManager _locationManager;
        readonly Lazy<IObservable<LocationRecorded>> _startListeningForLocationChanges;

        CLLocationManager Manager => _locationManager;


        public IObservable<LocationRecorded> StartListeningForLocationChanges => _startListeningForLocationChanges.Value;

        readonly ReplaySubject<bool> _isListeningForChangesObs;
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

            _isListeningForChangesObs = new ReplaySubject<bool>(1);
            _isListeningForChangesObs.OnNext(false);

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

                    Observable.FromEventPattern<EventHandler<NSErrorEventArgs>, NSErrorEventArgs>(
                            x => _locationManager.Failed += x,
                            x => _locationManager.Failed -= x
                            )
                            .Select(e => e.EventArgs)
                            .Subscribe(args =>
                            {
                                _exceptionHandling.LogException(
                                    new LocationActivationException(ActivationFailedReasons.Unknown, args.Error.Description)
                                );
                            });
                });



            _startListeningForLocationChanges =
                new Lazy<IObservable<LocationRecorded>>(() =>
                {                    
                    var checkPermission =
                        Observable.Defer(() => 
                            _permissionProvider.CheckLocationPermission()
                        );


                    var startLocationUpdates =
                        Observable.Create<LocationRecorded>(subj =>
                        {   
                            CompositeDisposable disp = new CompositeDisposable();

                            try
                            {
                                Manager.StartUpdatingLocation();
                                IsListeningForChangesImperative = true;
                                disp.Add(
                                    Observable.FromEventPattern<EventHandler<CLLocationsUpdatedEventArgs>, CLLocationsUpdatedEventArgs>(
                                        x => Manager.LocationsUpdated += x,
                                        x => Manager.LocationsUpdated -= x
                                        )
                                        .SelectMany(lu => lu.EventArgs.Locations)
                                        .Select(lu => createPositionFromPlatform(lu))
                                        .Where(lu => lu != null)
                                        .Subscribe(subj)
                                );
                                 

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
                            catch(Exception exc)
                            {                                
                                subj.OnError(exc);
                                disp.Dispose();
                            }

                            return Disposable.Empty;
                        })
                        .SubscribeOn(_scheduler.Dispatcher);

                    return
                        checkPermission
                            .SelectMany(_ => startLocationUpdates)
                            .Catch((Exception exc) =>
                            {
                                _exceptionHandling.LogException(exc);
                                return Observable.Empty<LocationRecorded>();
                            })
                            .Publish()
                            .RefCount();
                }
            );
        }

        public IObservable<bool> IsListeningForChanges => _isListeningForChangesObs.AsObservable().DistinctUntilChanged();

        bool IsListeningForChangesImperative
        {
            get
            {
                return _isListeningForChangesImperative;
            }
            set
            {
                _isListeningForChangesImperative = value;
                _isListeningForChangesObs.OnNext(value);
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