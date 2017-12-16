using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xam.Reactive.Location
{
    // ?move to Factory instead of partial?
    public partial class LocationListener
    {        
        public static LocationListener Create
        (
            IExceptionHandlerService exceptionHandler = null,
            ISchedulerFactory scheduler = null,
            ICheckPermissionProvider permissionProvider = null
        )
        {
            scheduler = scheduler ?? CreateScheduler();
            exceptionHandler = exceptionHandler ?? CreateExceptionHandler();
            permissionProvider = permissionProvider ?? CreatePermissionProvider(scheduler);
            return new LocationListener(permissionProvider, exceptionHandler, scheduler);
        }


        static ISchedulerFactory CreateScheduler() => new XamarinSchedulerFactory();
        static IExceptionHandlerService CreateExceptionHandler() => new ExceptionHandlerService();
        static ICheckPermissionProvider CreatePermissionProvider(ISchedulerFactory scheduler = null) 
            => new BestGuessCheckPermissionProvider(scheduler ?? CreateScheduler());
    }
}
