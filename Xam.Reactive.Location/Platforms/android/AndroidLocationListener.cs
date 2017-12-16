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
using Xam.Reactive.Concurrency;

namespace Xam.Reactive.Location
{
    [Preserve]
    public partial class LocationListener :  LocationListenerBase<Android.Locations.Location>
    {
        static DateTimeOffset baseAndroidTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private LocationRequest mLocationRequest;
        private LocationCallbackImpl _myCallback; 
        private FusedLocationProviderClient mFusedLocationClient; 


        public LocationListener(
            ICheckPermissionProvider permissionProvider,
            IExceptionHandlerService exceptionHandling,
            ISchedulerFactory scheduler,
            LocationRequest locationRequest = null) : base(permissionProvider, exceptionHandling, scheduler)
        {
            mLocationRequest = locationRequest;
            mFusedLocationClient = LocationServices.GetFusedLocationProviderClient(GetContext()); 
            _myCallback = new LocationCallbackImpl();
            PositionFactory = createPositionFromPlatform;
        }

        LocationAvailability Availability
        {
            get;
            set;
        }

        protected override IObservable<LocationRecorded> CreateStartLocationUpdates()
        {
            return Observable.Create<LocationRecorded>(subj =>
            {
                CompositeDisposable disp = new CompositeDisposable();
                try
                { 
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
                            .Select(lu => PositionFactory(lu))
                            .Where(lu => lu != null)
                            .StartWith((LocationRecorded)null)
                            .Catch((Exception exc) =>
                            {
                                return Observable.Throw<LocationRecorded>(exc);
                            });

                    var removeLocationUpdates = 
                        Observable.Create<Unit>(subs =>
                        {
                            return 
                                mFusedLocationClient
                                    .RemoveLocationUpdatesAsync(_myCallback)
                                    .ToObservable()
                                    .CatchAndLog(ExceptionHandling, Unit.Default)
                                    .Subscribe(subs);
                        })
                        .SubscribeOn(Scheduler.Dispatcher);

                    // GetLocationAvailabilityAsync() throws an ApiException
                    // if google play services aren't available
                    // so I don't currently see the point of checking that before requesting location updates
                    // since I feel like they will achieve the same thing at this point
                    // need to investigate where there might be divergent behavior between the two
                    Func<IObservable<bool>> requestUpdates = null;
                    requestUpdates = () =>
                        // using from async to ensure it's lazy
                        Observable.FromAsync(() => mFusedLocationClient
                                .RequestLocationUpdatesAsync(mLocationRequest, _myCallback)
                             )
                            .Select(_ => true)
                            .Do(_=>
                            {
                                // successfully requesting updates so setup remove on unsubscribe.
                                // only want to wire this up if requesting updates is successful
                                Disposable.Create(() =>
                                {
                                    removeLocationUpdates
                                        .Subscribe();
                                })
                                .DisposeWith(disp);
                            })

                            // error trying to request updates
                            .Catch((ApiException api) =>
                            { 
                                var activationException =
                                    new LocationActivationException
                                    (
                                        ActivationFailedReasons.CheckExceptionOnPlatform,
                                        api
                                    );

                                ExceptionHandling
                                    .LogException(activationException);

                                // wait for a change in location availibility to occur
                                return
                                    Observable.FromEventPattern<LocationCallbackAvailabilityEventArgs>
                                        (
                                            x => _myCallback.LocationAvailability += x,
                                            x => _myCallback.LocationAvailability -= x
                                        )
                                        .Select(changed => changed.EventArgs.Availability.IsLocationAvailable)
                                        .Log("Availability")
                                        .Where(x=> x)
                                        .Take(1)
                                        .SelectMany(_=> requestUpdates());
                            })
                            // unknown exception occurred :-(
                            .Catch((Exception exc) =>
                            {
                                return Observable.Throw<bool>(exc);
                            })
                            .StartWith(false);

                    Observable.CombineLatest(
                            locationResults,
                            requestUpdates(),
                        (result, requestUpdatesActive) =>
                        {
                            IsListeningForChangesImperative = true;

                            if (!requestUpdatesActive)
                            {
                                return Observable.Empty<LocationRecorded>();
                            }

                            if (result == null)
                            {
                                return Observable.Empty<LocationRecorded>();
                            }

                            return Observable.Return(result);
                        })
                        .SelectMany(x => x)
                        .Subscribe(subj)
                        .DisposeWith(disp);

                    Disposable.Create(() =>
                    {
                        IsListeningForChangesImperative = false;
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
            .SubscribeOn(Scheduler.Dispatcher);
        }


        private LocationRecorded createPositionFromPlatform(Android.Locations.Location location)
        {   
            return    new LocationRecorded
                (
                    location.Latitude,
                    location.Longitude,
                    baseAndroidTime.AddMilliseconds(location.Time),
                    location.Accuracy
                );         
        }


        private Context GetContext()
        {
            // is this ok enough?
            return Application.Context;
        } 

    }
}