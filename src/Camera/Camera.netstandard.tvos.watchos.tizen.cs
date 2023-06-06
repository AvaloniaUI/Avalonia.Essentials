#nullable enable
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel;

namespace Microsoft.Maui.CameraDevice
{
	partial class CameraProviderImplementation : ICameraProvider
	{
		public IEnumerable<ICameraDevice> GetAvailableDevices()
		{
			throw ExceptionUtils.NotSupportedOrImplementedException;
		}
	}
}
