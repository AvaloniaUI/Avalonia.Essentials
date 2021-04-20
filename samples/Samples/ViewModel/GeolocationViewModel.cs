using System;
using System.Threading;
using System.Windows.Input;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Essentials;

namespace Samples.ViewModel
{
	public class GeolocationViewModel : BaseViewModel
	{
		string notAvailable = "not available";
		string lastLocation;
		string currentLocation;
		int accuracy = (int)GeolocationAccuracy.Default;
		CancellationTokenSource cts;

		public GeolocationViewModel()
		{
			GetLastLocationCommand = new Command(OnGetLastLocation);
			GetCurrentLocationCommand = new Command(OnGetCurrentLocation);
		}

		public ICommand GetLastLocationCommand { get; }

		public ICommand GetCurrentLocationCommand { get; }

		public string LastLocation
		{
			get => lastLocation;
			set => SetProperty(ref lastLocation, value);
		}

		public string CurrentLocation
		{
			get => currentLocation;
			set => SetProperty(ref currentLocation, value);
		}

		public string[] Accuracies
			=> Enum.GetNames(typeof(GeolocationAccuracy));

		public int Accuracy
		{
			get => accuracy;
			set => SetProperty(ref accuracy, value);
		}

		async void OnGetLastLocation()
		{
			if (IsBusy)
				return;

			IsBusy = true;
			try
			{
				var location = await Geolocation.GetLastKnownLocationAsync();
				LastLocation = FormatLocation(location);
			}
			catch (Exception ex)
			{
				LastLocation = FormatLocation(null, ex);
			}
			IsBusy = false;
		}

		async void OnGetCurrentLocation()
		{
			if (IsBusy)
				return;

			IsBusy = true;
			try
			{
				var request = new GeolocationRequest((GeolocationAccuracy)Accuracy);
				cts = new CancellationTokenSource();
				var location = await Geolocation.GetLocationAsync(request, cts.Token);
				CurrentLocation = FormatLocation(location);
			}
			catch (Exception ex)
			{
				CurrentLocation = FormatLocation(null, ex);
			}
			finally
			{
				cts.Dispose();
				cts = null;
			}
			IsBusy = false;
		}

		string FormatLocation(Location location, Exception ex = null)
		{
			if (location == null)
			{
				return $"Unable to detect location. Exception: {ex?.Message ?? string.Empty}";
			}

			return
				$"Latitude: {location.Latitude}\n" +
				$"Longitude: {location.Longitude}\n" +
				$"HorizontalAccuracy: {location.Accuracy}\n" +
				$"Altitude: {(location.Altitude.HasValue ? location.Altitude.Value.ToString() : notAvailable)}\n" +
				$"AltitudeRefSys: {location.AltitudeReferenceSystem.ToString()}\n" +
				$"VerticalAccuracy: {(location.VerticalAccuracy.HasValue ? location.VerticalAccuracy.Value.ToString() : notAvailable)}\n" +
				$"Heading: {(location.Course.HasValue ? location.Course.Value.ToString() : notAvailable)}\n" +
				$"Speed: {(location.Speed.HasValue ? location.Speed.Value.ToString() : notAvailable)}\n" +
				$"Date (UTC): {location.Timestamp:d}\n" +
				$"Time (UTC): {location.Timestamp:T}\n" +
				$"Moking Provider: {location.IsFromMockProvider}";
		}

		public override void OnDisappearing()
		{
			if (IsBusy)
			{
				if (cts != null && !cts.IsCancellationRequested)
					cts.Cancel();
			}
			base.OnDisappearing();
		}
	}
}
