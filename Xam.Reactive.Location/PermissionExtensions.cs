using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using Xam.Reactive.Location;

namespace Xam.Reactive
{
    public static class PermissionExtensions
    {

        public static IObservable<bool> CheckLocationPermission
        (
            this ICheckPermissionProvider permissionProvider, 
                 IExceptionHandlerService exceptionHandler
        ) =>
            permissionProvider
                .Location
                .Select(result =>
                {
                    if (!result)
                    {
                        exceptionHandler.LogException(new LocationActivationException(ActivationFailedReasons.PermissionsIssue));
                        return false;
                    }

                    return true;
                });
    }
}
