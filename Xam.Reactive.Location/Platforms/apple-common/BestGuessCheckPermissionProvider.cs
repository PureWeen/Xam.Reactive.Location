using CoreLocation;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Xam.Reactive
{
    public class BestGuessCheckPermissionProvider : ICheckPermissionProvider
    {
        public BestGuessCheckPermissionProvider()
        {

        }
        public BestGuessCheckPermissionProvider(ISchedulerFactory schedulerFactory)
        {
        }

        public virtual IObservable<bool> Location =>
            Observable.Create<bool>(sub =>
            {
                try
                {
                    switch (CLLocationManager.Status)
                    {
                        case CLAuthorizationStatus.Authorized:
                        case CLAuthorizationStatus.AuthorizedWhenInUse:
                            sub.OnNext(true);
                            break;
                        default:
                            sub.OnNext(false);
                            break;
                    }

                    sub.OnCompleted();
                }
                catch(Exception exc)
                {
                    sub.OnError(exc);
                }

                return Disposable.Empty;
            });
            
    }
}