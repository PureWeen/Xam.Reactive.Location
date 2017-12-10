using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace RxLocation.Sample
{
	public partial class App : Application
	{
        public MainPageViewModel MainViewModel { get; }
		public App ()
		{
			InitializeComponent();
			MainPage = new RxLocation.Sample.MainPage();
            MainViewModel = new MainPageViewModel();
            MainPage.BindingContext = MainViewModel;
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
