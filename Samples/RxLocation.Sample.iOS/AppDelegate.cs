using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using CoreLocation;
using Foundation;
using UIKit;
using Xam.Reactive;
using Xam.Reactive.Location;

namespace RxLocation.Sample.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            var xamApp = new App();
            var permissions = new iOSRequestPermissions();
            var errorHandler = new ExceptionHandlerService();
            var locationManager = LocationListener.CreateLocationManager();

            locationManager.AllowsBackgroundLocationUpdates = true;
            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                locationManager.ShowsBackgroundLocationIndicator = true;
            }

            var locationService = 
                LocationListener
                    .CreatePlatform(exceptionHandler: errorHandler, locationManager: locationManager);


            errorHandler
                .OnError
                .Select(x => x as LocationActivationException)
                .Where(x => x?.Reason == ActivationFailedReasons.PermissionsIssue)
                .SelectMany(_ => permissions.Location)
                .Subscribe();

            xamApp
                .MainViewModel
                .SetLocationService(locationService);

            LoadApplication(xamApp);

            return base.FinishedLaunching(app, options);
        }
    }


    public class iOSRequestPermissions
    {
        // needs to be class level for requesting authorizations
        // otherwise it gets collected and dialog vanishes
        // https://stackoverflow.com/questions/7888896/current-location-permission-dialog-disappears-too-quickly
        Lazy<CLLocationManager> _manager;
        public iOSRequestPermissions()
        {
            _manager = new Lazy<CLLocationManager>(() => new CLLocationManager());
        }

        public IObservable<Unit> Location
        {
            get
            {
                // ios suggests first asking for when in use
                // then following up with hey thank you
                // can we also do always to make life even better?
                switch (CLLocationManager.Status)
                {
                    case CLAuthorizationStatus.AuthorizedAlways:
                        return Observable.Return(Unit.Default);

                    case CLAuthorizationStatus.AuthorizedWhenInUse:
                        _manager.Value.RequestAlwaysAuthorization();
                        return Observable.Return(Unit.Default);
                    default:

                        _manager.Value.RequestWhenInUseAuthorization();                        
                        Observable.FromEventPattern<CLAuthorizationChangedEventArgs>
                        (
                            x => _manager.Value.AuthorizationChanged += x,
                            x => _manager.Value.AuthorizationChanged -= x
                        )
                        .Select(x => x.EventArgs.Status)
                        .Log("PermissionChanged")
                        .Where(Status => Status == CLAuthorizationStatus.AuthorizedWhenInUse)
                        .Take(1)
                        .Subscribe(_=>
                        {
                            _manager.Value.RequestAlwaysAuthorization();
                        });

                        return Observable.Return(Unit.Default);
                }
            }
        }
    }
}
