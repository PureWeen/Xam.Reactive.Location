using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Xamarin.DispatchScheduler;

namespace Xam.Reactive.Location
{
    [Preserve]
    public partial class LocationListener : 
        ILocationListener
    {
        static DateTimeOffset baseAndroidTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Lazy<IObservable<LocationRecorded>> _startListeningForLocationChanges;
        private LocationRequest mLocationRequest;

        public LocationCallbackImpl _myCallback;
        private Subject<LocationRecorded> positions { get; } = new Subject<LocationRecorded>();

        readonly BehaviorSubject<bool> _isListeningForChangesObs;
        bool _isListeningForChangesImperative;

        readonly ICheckPermissionProvider _permissionProvider;
        readonly IExceptionHandlerService _exceptionHandling;
        readonly ISchedulerFactory _scheduler;

        private FusedLocationProviderClient mFusedLocationClient;

        public LocationListener(
            ICheckPermissionProvider permissionProvider,
            IExceptionHandlerService exceptionHandling,
            ISchedulerFactory scheduler)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _exceptionHandling = exceptionHandling ?? throw new ArgumentNullException(nameof(exceptionHandling));
            _permissionProvider = permissionProvider ?? throw new ArgumentNullException(nameof(permissionProvider));

            mFusedLocationClient = LocationServices.GetFusedLocationProviderClient(GetContext()); 
            _isListeningForChangesObs = new BehaviorSubject<bool>(false); 

            _startListeningForLocationChanges =
               new Lazy<IObservable<LocationRecorded>>(createStartListeningForLocationChanges);


            _myCallback = new LocationCallbackImpl();


        }

        private IObservable<LocationRecorded> createStartListeningForLocationChanges()
        {
            return
                Observable.Defer(() =>
                    _permissionProvider
                        .CheckLocationPermission()
                        .SelectMany(_ => createStartLocationUpdates())
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

        LocationAvailability Availability
        {
            get;
            set;
        }

        private IObservable<LocationRecorded> createStartLocationUpdates()
        {
            return Observable.Create<LocationRecorded>(subj =>
            {

                CompositeDisposable disp = new CompositeDisposable();
                try
                {
                    mLocationRequest = OnCreateLocationRequest();
                    if (mLocationRequest == null)
                    {
                        mLocationRequest = LocationRequest.Create();
                        mLocationRequest.SetPriority(LocationRequest.PriorityBalancedPowerAccuracy);
                        mLocationRequest.SetInterval(10000);
                        mLocationRequest.SetFastestInterval(5000);
                    }

                    var locationResults =
                        Observable.FromEventPattern<LocationCallbackResultEventArgs>
                            (
                                x => _myCallback.LocationResult += x,
                                x => _myCallback.LocationResult -= x
                            )
                            .SelectMany(lu => lu.EventArgs.Result.Locations)
                            .Select(lu => createPositionFromPlatform(lu))
                            .Where(lu => lu != null)
                            .StartWith((LocationRecorded)null);


                    var availabilityChanged =
                        Observable.FromEventPattern<LocationCallbackAvailabilityEventArgs>
                            (
                                x => _myCallback.LocationAvailability += x,
                                x => _myCallback.LocationAvailability -= x
                            )
                            .Select(lu => lu.EventArgs.Availability)
                            .StartWith((LocationAvailability)null);

                    var getAvailability =
                        mFusedLocationClient
                            .GetLocationAvailabilityAsync()
                            .ToObservable()
                            .StartWith((LocationAvailability)null);

                    var requestUpdates =
                        // using from async to ensure it's lazy
                        Observable.FromAsync(() => mFusedLocationClient
                                .RequestLocationUpdatesAsync(mLocationRequest, _myCallback)
                             );


                    Observable.CombineLatest(
                            locationResults,
                            availabilityChanged,
                            getAvailability,
                            requestUpdates,
                        (result, availabilityEvent, currentAvailability, startRequest) =>
                        {
                            LocationAvailability availibility =
                                availabilityEvent ?? currentAvailability;
                            Availability = availibility;

                            if (availibility == null && result == null)
                            {
                                return Observable.Empty<LocationRecorded>();
                            }

                            if (availibility != null && !availibility.IsLocationAvailable)
                            {
                                var activationException =
                                   new LocationActivationException
                                    (
                                        ActivationFailedReasons.LocationServicesNotAvailable
                                   );

                                return Observable.Throw<LocationRecorded>(activationException);
                            }

                            if (result == null)
                            {
                                return Observable.Empty<LocationRecorded>();
                            }

                            IsListeningForChangesImperative = true;
                            return Observable.Return(result);
                        })
                        .SelectMany(x => x)
                        .Catch((ApiException api) =>
                        {
                            var activationException =
                               new LocationActivationException
                                (
                                    ActivationFailedReasons.CheckExceptionOnPlatform,
                                   api
                               );

                            return Observable.Throw<LocationRecorded>(activationException);
                        })
                        .Subscribe(subj)
                        .DisposeWith(disp);

                    Disposable.Create(() =>
                    {
                        _scheduler
                            .Dispatcher
                            .Schedule(() =>
                            {
                                try
                                {
                                    mFusedLocationClient
                                         .RemoveLocationUpdatesAsync(_myCallback)
                                         .ToObservable()
                                         .CatchAndLog(_exceptionHandling, Unit.Default)
                                         .Subscribe();
                                }
                                catch (Exception exc)
                                {
                                    _exceptionHandling.LogException(exc);
                                }
                                finally
                                {
                                    IsListeningForChangesImperative = false;
                                }
                            });
                    })
                    .DisposeWith(disp);
                    return disp;

                }
                catch (Exception exc)
                {
                    subj.OnError(exc);
                    disp.Dispose();
                }

                return Disposable.Empty;
            }
            )
            .SubscribeOn(_scheduler.Dispatcher);
        }

        private LocationRecorded createPositionFromPlatform(Android.Locations.Location location)
        {
            var thePosition = OnCreatePositionFromPlatform(location);
            if (thePosition == null)
            {
                thePosition =
                    new LocationRecorded
                    (
                        location.Latitude,
                        location.Longitude,
                        baseAndroidTime.AddMilliseconds(location.Time),
                        location.Accuracy
                    );
            }

            return thePosition;
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
                if (_isListeningForChangesImperative != value)
                {
                    _isListeningForChangesImperative = value;
                    _isListeningForChangesObs.OnNext(value);
                }
            }
        }

         

        public IObservable<LocationRecorded> StartListeningForLocationChanges => 
            _startListeningForLocationChanges.Value;

       

        public virtual LocationRequest OnCreateLocationRequest()
        {
            return null;
        }

        public virtual LocationRecorded OnCreatePositionFromPlatform(Android.Locations.Location location)
        {
            return null;
        }
          



        private Context GetContext()
        {
            // is this ok enough?
            return Application.Context;
        } 


    }
}