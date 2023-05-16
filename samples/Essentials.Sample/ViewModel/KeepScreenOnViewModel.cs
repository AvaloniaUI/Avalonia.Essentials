using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices;

namespace Samples.ViewModel
{
	public class KeepScreenOnViewModel : BaseViewModel
	{
		public KeepScreenOnViewModel()
		{
			RequestActiveCommand = new RelayCommand(OnRequestActive);
			RequestReleaseCommand = new RelayCommand(OnRequestRelease);
		}

		public bool IsActive => DeviceDisplay.KeepScreenOn;

		public ICommand RequestActiveCommand { get; }

		public ICommand RequestReleaseCommand { get; }

		public override void OnDisappearing()
		{
			OnRequestRelease();

			base.OnDisappearing();
		}

		void OnRequestActive()
		{
			DeviceDisplay.KeepScreenOn = true;

			OnPropertyChanged(nameof(IsActive));
		}

		void OnRequestRelease()
		{
			DeviceDisplay.KeepScreenOn = false;

			OnPropertyChanged(nameof(IsActive));
		}
	}
}
