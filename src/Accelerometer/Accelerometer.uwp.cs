using Windows.Devices.Sensors;
using WindowsAccelerometer = Windows.Devices.Sensors.Accelerometer;

namespace Microsoft.Maui.Devices.Sensors
{
	partial class WinAccelerometerImplementation : AccelerometerImplementationBase
	{
		// keep around a reference so we can stop this same instance
		WindowsAccelerometer sensor;

		internal static WindowsAccelerometer DefaultSensor =>
			WindowsAccelerometer.GetDefault();

		public override bool IsSupported =>
			DefaultSensor != null;

		protected override void PlatformStart(SensorSpeed sensorSpeed)
		{
			sensor = DefaultSensor;

			var interval = sensorSpeed.ToPlatform();
			sensor.ReportInterval = sensor.MinimumReportInterval >= interval ? sensor.MinimumReportInterval : interval;

			sensor.ReadingChanged += DataUpdated;
		}

		void DataUpdated(object sender, AccelerometerReadingChangedEventArgs e)
		{
			var reading = e.Reading;
			var data = new AccelerometerData(reading.AccelerationX * -1, reading.AccelerationY * -1, reading.AccelerationZ * -1);
			OnChanged(data);
		}

		protected override void PlatformStop()
		{
			sensor.ReadingChanged -= DataUpdated;
			sensor.ReportInterval = 0;
		}
	}
}
