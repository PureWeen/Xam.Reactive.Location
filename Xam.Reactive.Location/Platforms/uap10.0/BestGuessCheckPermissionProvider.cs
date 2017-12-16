using System;
using System.Reactive.Linq;
using Windows.Devices.Geolocation;

namespace Xam.Reactive
{
    public class BestGuessCheckPermissionProvider : ICheckPermissionProvider
    {
        public BestGuessCheckPermissionProvider(ISchedulerFactory schedulerFactory)
        {
            SchedulerFactory = schedulerFactory;
        }


        public IObservable<bool> Location
        {
            get
            {
                return 
                    Observable.Create<bool>(async sub =>
                    {
                        try
                        {
                            var accessStatus = await Geolocator.RequestAccessAsync();

                            switch (accessStatus)
                            {
                                case GeolocationAccessStatus.Allowed:
                                    sub.OnNext(true);
                                    sub.OnCompleted();
                                    break;
                                case GeolocationAccessStatus.Denied:
                                case GeolocationAccessStatus.Unspecified:
                                    sub.OnNext(false);
                                    sub.OnCompleted();
                                    break;
                                default:
                                    sub.OnError(new ArgumentException($"Unknown Access:  {accessStatus}"));
                                    break;
                            }
                        }
                        catch(Exception exc)
                        {
                            sub.OnError(exc);
                        }

                    })
                    .SubscribeOn(SchedulerFactory.Dispatcher);
            }
        }

        ISchedulerFactory SchedulerFactory { get; }
    }
}