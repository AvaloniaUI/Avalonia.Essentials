using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace Samples.ViewModel
{
	public class HapticFeedbackViewModel : BaseViewModel
	{
		bool isSupported = true;

		public HapticFeedbackViewModel()
		{
			ClickCommand = new RelayCommand(OnClick);
			LongPressCommand = new RelayCommand(OnLongPress);
		}

		public ICommand ClickCommand { get; }

		public ICommand LongPressCommand { get; }

		public bool IsSupported
		{
			get => isSupported;
			set => SetProperty(ref isSupported, value);
		}

		void OnClick()
		{
			try
			{
				HapticFeedback.Perform(HapticFeedbackType.Click);
			}
			catch (FeatureNotSupportedException)
			{
				IsSupported = false;
			}
			catch (Exception ex)
			{
				DisplayAlertAsync($"Unable to HapticFeedback: {ex.Message}");
			}
		}

		void OnLongPress()
		{
			try
			{
				HapticFeedback.Perform(HapticFeedbackType.LongPress);
			}
			catch (FeatureNotSupportedException)
			{
				IsSupported = false;
			}
			catch (Exception ex)
			{
				DisplayAlertAsync($"Unable to HapticFeedback: {ex.Message}");
			}
		}
	}
}
