
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
    public partial class LocationListener : LocationListenerBase<Geoposition>
    {
        private Geolocator _geolocator;

        public LocationListener(
            ICheckPermissionProvider permissionProvider,
            IExceptionHandlerService exceptionHandling,
            ISchedulerFactory scheduler, 
            Geolocator geoLocator = null) : base(permissionProvider, exceptionHandling, scheduler)
        {
            _geolocator = geoLocator;
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
                            
                               Observable.FromEventPattern<TypedEventHandler<Geolocator, StatusChangedEventArgs>, StatusChangedEventArgs>
                                    (
                                        x => _geolocator.StatusChanged += x,
                                        x => _geolocator.StatusChanged -= x
                                    )
                                    .Select(lu => lu.EventArgs.Status)
                                    .Log("StatusChanged")
                                    .Subscribe(statusChanged =>
                                    {
                                        switch(statusChanged)
                                        {
                                            case PositionStatus.Disabled:
                                                break;
                                            case PositionStatus.Initializing:
                                                break;
                                            case PositionStatus.NoData:
                                                break;
                                            case PositionStatus.NotAvailable:
                                                break;
                                            case PositionStatus.NotInitialized:
                                                break;
                                            case PositionStatus.Ready:
                                                break;
                                        }
                                    })
                                    .DisposeWith(disp);


                           getCurrentPosition
                                .Merge(listenForChanges)
                                .Where(lu => lu != null)
                                .Select(lu => PositionFactory(lu))
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
                   .SubscribeOn(Scheduler.Dispatcher);
        }


        private LocationRecorded createPositionFromPlatform(Geoposition location)
        { 
            return new LocationRecorded
            (
                location.Coordinate.Point.Position.Latitude,
                location.Coordinate.Point.Position.Longitude,
                location.Coordinate.Timestamp,
                (float)location.Coordinate.Accuracy
            ); 
        }
 
    }
}