#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Maui.Essentials
{
	partial class DeviceDisplayImplementation : IDeviceDisplay
	{
		[DllImport("libcapi-system-device.so.0", EntryPoint = "device_power_request_lock")]
		static extern void RequestKeepScreenOn(int type = 1, int timeout = 0);

		[DllImport("libcapi-system-device.so.0", EntryPoint = "device_power_release_lock")]
		static extern void ReleaseKeepScreenOn(int type = 1);

		bool keepScreenOn = false;

		public event EventHandler<DisplayInfoChangedEventArgs>? MainDisplayInfoChanged;

		public bool KeepScreenOn
		{
			get => keepScreenOn;
			set
			{
				if (value)
					RequestKeepScreenOn();
				else
					ReleaseKeepScreenOn();
				keepScreenOn = value;
			}
		}

		public DisplayInfo GetMainDisplayInfo()
		{
			var display = Platform.MainWindow;
			return new DisplayInfo(
				width: display.ScreenSize.Width,
				height: display.ScreenSize.Height,
				density: display.ScreenDpi.X / (DeviceInfo.Idiom == DeviceIdiom.TV ? 72.0 : 160.0),
				orientation: GetOrientation(),
				rotation: GetRotation());
		}

		static DisplayOrientation GetOrientation()
		{
			return Platform.MainWindow.Rotation switch
			{
				0 => DisplayOrientation.Portrait,
				90 => DisplayOrientation.Landscape,
				180 => DisplayOrientation.Portrait,
				270 => DisplayOrientation.Landscape,
				_ => DisplayOrientation.Unknown,
			};
		}

		static DisplayRotation GetRotation()
		{
			return Platform.MainWindow.Rotation switch
			{
				0 => DisplayRotation.Rotation0,
				90 => DisplayRotation.Rotation90,
				180 => DisplayRotation.Rotation180,
				270 => DisplayRotation.Rotation270,
				_ => DisplayRotation.Unknown,
			};
		}

		public void StartScreenMetricsListeners()
		{
			Platform.MainWindow.RotationChanged += OnRotationChanged;
		}

		public void StopScreenMetricsListeners()
		{
			Platform.MainWindow.RotationChanged -= OnRotationChanged;
		}

		void OnRotationChanged(object s, EventArgs e)
		{
			var metrics = GetMainDisplayInfo();
			MainDisplayInfoChanged?.Invoke(this, new DisplayInfoChangedEventArgs(metrics));
		}
	}
}
