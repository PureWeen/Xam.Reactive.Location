using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Xam.Reactive.Location;
using Xamarin.DispatchScheduler;
using System.Reactive.Concurrency;

namespace RxLocation.Sample
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        LocationService _locationServiceCrossPlatformSimple;
        
        public MainPageViewModel()
        {
            _locationServiceCrossPlatformSimple = LocationService.CreateWithDefaults();
            _locationServiceCrossPlatformSimple
                .OnError
                .Subscribe(exc =>
                {
                    Error = exc.ToString();
                });

            SerialDisposable disp = new SerialDisposable();
            ToggleListeningForChanges = new SimpleCommand(() =>
            {
                if(!IsListeningForLocationChanges)
                {
                    disp.Disposable = 
                        _locationServiceCrossPlatformSimple
                            .StartListeningForLocationChanges
                            .SubscribeOn(TaskPoolScheduler.Default)
                            .ObserveOn(XamarinDispatcherScheduler.Current)
                            .Subscribe(DisplayPosition);
                }
                else
                {
                    disp.Disposable = Disposable.Empty;
                }
            });


            GetCurrentPosition = new SimpleCommand(() =>
            {
                _locationServiceCrossPlatformSimple
                    .GetDeviceLocation(10000)
                    .SubscribeOn(TaskPoolScheduler.Default)
                    .ObserveOn(XamarinDispatcherScheduler.Current)
                    .Catch((TimeoutException te) =>
                    {
                        LocationChanged = "Time out waiting for location change";
                        return Observable.Empty<LocationRecorded>();
                    })
                    .Subscribe(DisplayPosition);
            });


            _locationServiceCrossPlatformSimple
                .IsListeningForChanges
                .Subscribe(isListening => IsListeningForLocationChanges = isListening);
        }


        void DisplayPosition(LocationRecorded position)
        {
            LocationChanged = $"{position.Recorded} {position.Longitude}, {position.Latitude}";

        }


        bool _IsListeningForLocationChanges;
        public bool IsListeningForLocationChanges
        {
            get => _IsListeningForLocationChanges;
            set => OnPropertyChanged(nameof(IsListeningForLocationChanges), value, ref _IsListeningForLocationChanges);
        }

        string _locationChanged;
        public string LocationChanged
        {
            get => _locationChanged;
            set => OnPropertyChanged(nameof(LocationChanged), value, ref _locationChanged);
        }

        string _error;
        public string Error
        {
            get => _error;
            set => OnPropertyChanged(nameof(Error), value, ref _error);
        }


        public ICommand ToggleListeningForChanges { get; }

        public ICommand GetCurrentPosition { get; }


        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged<T>(string propertyName, T newValue, ref T propertyValue)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            propertyValue = newValue;
        }
    }



    public class SimpleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        Action _action;

        public SimpleCommand(Action action)
        {
            _action = action;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action();
        }
    }
}
