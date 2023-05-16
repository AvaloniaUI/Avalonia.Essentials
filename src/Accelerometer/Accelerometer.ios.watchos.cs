#nullable enable
using CoreMotion;
using Foundation;
using Microsoft.Maui.ApplicationModel;

namespace Microsoft.Maui.Devices.Sensors
{
	partial class AccelerometerImplementation : AccelerometerImplementationBase
	{
		static CMMotionManager? motionManager;

		static CMMotionManager MotionManager =>
			motionManager ??= new CMMotionManager();

		public override bool IsSupported =>
			MotionManager.AccelerometerAvailable;

		protected override void PlatformStart(SensorSpeed sensorSpeed)
		{
			MotionManager.AccelerometerUpdateInterval = sensorSpeed.ToPlatform();
			MotionManager.StartAccelerometerUpdates(NSOperationQueue.CurrentQueue ?? new NSOperationQueue(), DataUpdated);
		}

		void DataUpdated(CMAccelerometerData data, NSError error)
		{
			if (data == null)
				return;

#pragma warning disable CA1416 // https://github.com/xamarin/xamarin-macios/issues/14619
			var field = data.Acceleration;
#pragma warning restore CA1416
			var accelData = new AccelerometerData(field.X * -1, field.Y * -1, field.Z * -1);
			OnChanged(accelData);
		}

		protected override void PlatformStop() =>
			MotionManager.StopAccelerometerUpdates();
	}
}
