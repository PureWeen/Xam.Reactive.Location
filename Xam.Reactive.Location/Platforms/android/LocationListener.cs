using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.OS;
using Android.Runtime;
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
    public partial class LocationListener : Java.Lang.Object,
        Android.Gms.Location.ILocationListener,
        GoogleApiClient.IConnectionCallbacks,
        GoogleApiClient.IOnConnectionFailedListener,
        ILocationListener
    {
        static DateTimeOffset baseAndroidTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Lazy<IObservable<LocationRecorded>> _watchForPositionChanges;
        private LocationRequest mLocationRequest;
        private GoogleApiClient googleApiClient;
        private Subject<LocationRecorded> positions { get; } = new Subject<LocationRecorded>();


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

            _watchForPositionChanges =
               new Lazy<IObservable<LocationRecorded>>(() =>
               {
                   var checkPermission =
                       Observable.Defer(
                           () =>
                               _permissionProvider
                                   .Location
                                   .Where(hasPermission => hasPermission)
                           );


                   var startLocationUpdates =
                       Observable.Create<LocationRecorded>(subj =>
                       {
                           StartUpdates();
                           var disp = positions.Subscribe(subj);
                           return Disposable.Create(() =>
                           {
                               disp.Dispose();
                               StopUpdates();
                           });
                       })
                       .SubscribeOn(_scheduler.Dispatcher);

                   return
                       checkPermission
                           .SelectMany(_ => startLocationUpdates)
                           .Publish()
                           .RefCount();
               });
        }
         




        public bool IsListeningForChanges { get; private set; }
        public IObservable<LocationRecorded> WatchForPositionChanges => _watchForPositionChanges.Value;

        public void OnLocationChanged(Android.Locations.Location location)
        {
            var thePosition = OnCreatePositionFromPlatform(location);
            if(thePosition != null)
            {
                return;
            }
            else
            {
                thePosition =
                    new LocationRecorded(
                        location.Latitude,
                        location.Longitude,
                        baseAndroidTime.AddMilliseconds(location.Time),
                        location.Accuracy); 
            }



            positions.OnNext(thePosition);
        }


        public void OnConnected(Bundle connectionHint)
        {
            try
            {
                if (servicesConnected() && !IsListeningForChanges)
                {
                    LocationServices.FusedLocationApi.RequestLocationUpdates(googleApiClient, mLocationRequest, this);
                    IsListeningForChanges = true;
                }
            }
            catch (Exception exc)
            {
                if (!_exceptionHandling.LogException(exc)) throw;
            }
        }

        public void OnConnectionSuspended(int cause)
        {

        }

        public void OnConnectionFailed(ConnectionResult result)
        {

        }


        


        void StartUpdates()
        {
            try
            {
                _scheduler
                    .Dispatcher
                    .Schedule(() =>
                    {
                        setUpLocationClientIfNeeded();
                        if (servicesConnected() && !IsListeningForChanges)
                        {
                            LocationServices
                                .FusedLocationApi
                                .RequestLocationUpdates(googleApiClient, mLocationRequest, this);

                            IsListeningForChanges = true;
                        }
                    });
            }
            catch (Exception exc)
            {
                if (!_exceptionHandling.LogException(exc)) throw;
            }
        }


        void StopUpdates()
        {
            _scheduler
                .Dispatcher
                .Schedule(() =>
                {
                    try
                    {
                        IsListeningForChanges = false;
                        if (servicesConnected())
                        {
                            LocationServices
                                .FusedLocationApi
                                .RemoveLocationUpdates(googleApiClient, this);

                            googleApiClient?.Disconnect();
                        }
                    }
                    catch (Exception exc)
                    {
                        if (!_exceptionHandling.LogException(exc)) throw;
                    }
                });

        }
 


        private void setUpLocationClientIfNeeded()
        {
            if (googleApiClient == null)
                buildGoogleApiClient();

            if (!servicesConnected())
            {
                googleApiClient.Connect();
            }
        }

        public virtual LocationRequest OnCreateLocationRequest()
        {
            return null;
        }

        public virtual LocationRecorded OnCreatePositionFromPlatform(Android.Locations.Location location)
        {
            return null;
        }
         

        void buildGoogleApiClient()
        {
            googleApiClient =
                new GoogleApiClient.Builder(GetContext())
                    .AddConnectionCallbacks(this)
                    .AddOnConnectionFailedListener(this)
                    .AddApi(LocationServices.API)
                    .Build();

            mLocationRequest = OnCreateLocationRequest();

            if (mLocationRequest == null)
            {
                mLocationRequest = LocationRequest.Create();
                mLocationRequest.SetPriority(LocationRequest.PriorityBalancedPowerAccuracy);
                mLocationRequest.SetInterval(10000);
                mLocationRequest.SetFastestInterval(5000);
            }

            googleApiClient.Connect();
        }



        private Context GetContext()
        {
            return Application.Context;
        }

        private bool servicesConnected()
        {
            // Check that Google Play services is available
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(GetContext());

            // If Google Play services is available
            if (ConnectionResult.Success == resultCode)
            {
                return googleApiClient.IsConnected;
            }
            else
            {
                return false;
            }
        }


    }
}