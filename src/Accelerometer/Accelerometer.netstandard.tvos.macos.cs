using Microsoft.Maui.ApplicationModel;

namespace Microsoft.Maui.Devices.Sensors
{
	class NotImplementedAccelerometerImplementation : AccelerometerImplementationBase
	{
		public override bool IsSupported =>
			throw ExceptionUtils.NotSupportedOrImplementedException;

		protected override void PlatformStart(SensorSpeed sensorSpeed) =>
			throw ExceptionUtils.NotSupportedOrImplementedException;

		protected override void PlatformStop() =>
			throw ExceptionUtils.NotSupportedOrImplementedException;
	}
}
