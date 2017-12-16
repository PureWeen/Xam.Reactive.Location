using System;
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

        public IObservable<bool> Location => Observable.Return(true);
    }
}