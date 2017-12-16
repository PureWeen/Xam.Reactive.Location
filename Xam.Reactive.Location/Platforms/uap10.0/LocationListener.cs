
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
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Xam.Reactive.Concurrency;

namespace Xam.Reactive.Location
{
    public partial class LocationListener : 
        ILocationListener
    {
        Lazy<IObservable<LocationRecorded>> _startListeningForLocationChanges;
        private Subject<LocationRecorded> positions { get; } = new Subject<LocationRecorded>();

        readonly BehaviorSubject<bool> _isListeningForChangesObs;
        bool _isListeningForChangesImperative;
        private Geolocator _geolocator;
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
            _startListeningForLocationChanges =
               new Lazy<IObservable<LocationRecorded>>(createStartListeningForLocationChanges);
        }

        private IObservable<LocationRecorded> createStartListeningForLocationChanges()
        {
            return
                Observable.Defer(() =>
                    _permissionProvider
                        .CheckLocationPermission(_exceptionHandling)
                        .SelectMany(_ => 
                            createStartLocationUpdates()
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

        private IObservable<LocationRecorded> createStartLocationUpdates()
        {
            return
               Observable
                   .Create<LocationRecorded>(subj =>
                   {
                       CompositeDisposable disp = new CompositeDisposable();
                       try
                       {
                           IsListeningForChangesImperative = true;
                           _geolocator = OnCreateGeolocator();
                           if (_geolocator == null)
                           {
                               _geolocator = new Geolocator { ReportInterval = 10000 };
                           }

                           Disposable.Create(() =>
                           {                               
                               IsListeningForChangesImperative = false;
                           })
                           .DisposeWith(disp);

                           var getCurrentPosition =
                                Observable.FromAsync(() => _geolocator.GetGeopositionAsync().AsTask());

                           var listenForChanges = 
                               Observable.FromEventPattern<TypedEventHandler<Geolocator, PositionChangedEventArgs>, PositionChangedEventArgs >
                                   (
                                       x => _geolocator.PositionChanged += x,
                                       x => _geolocator.PositionChanged -= x
                                   )
                                   .Select(lu => lu.EventArgs.Position);

                          // var statusChanged = 
                               Observable.FromEventPattern<TypedEventHandler<Geolocator, StatusChangedEventArgs>, StatusChangedEventArgs>
                                    (
                                        x => _geolocator.StatusChanged += x,
                                        x => _geolocator.StatusChanged -= x
                                    )
                                    .Select(lu => lu.EventArgs.Status)
                                    .Log("StatusChanged")
                                    .Subscribe()
                                    .DisposeWith(disp);


                           getCurrentPosition
                                .Concat(listenForChanges)
                                .Where(lu => lu != null)
                                .Select(lu => createPositionFromPlatform(lu))
                                .Subscribe(subj)
                                .DisposeWith(disp);

                           return disp;
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

        public virtual Geolocator OnCreateGeolocator()
        {
            return null;
        }

        private LocationRecorded createPositionFromPlatform(Geoposition location)
        {
            var thePosition = OnCreatePositionFromPlatform(location);
            if (thePosition == null)
            {
                thePosition =
                    new LocationRecorded
                    (
                        location.Coordinate.Point.Position.Latitude,
                        location.Coordinate.Point.Position.Longitude,
                        location.Coordinate.Timestamp,
                        (float)location.Coordinate.Accuracy
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

       
         

        public virtual LocationRecorded OnCreatePositionFromPlatform(Geoposition location)
        {
            return null;
        }
          


    }
}