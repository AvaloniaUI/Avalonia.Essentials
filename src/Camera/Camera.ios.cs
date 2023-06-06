using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace Microsoft.Maui.CameraDevice
{
	public partial class CameraProviderImplementation : ICameraProvider
	{
		public CameraProviderImplementation()
		{
		}

		public IEnumerable<ICameraDevice> GetAvailableDevices()
		{
			throw ExceptionUtils.NotSupportedOrImplementedException;
		}
	}

	public class CameraDeviceImpl : ICameraDevice
	{
		public string Id { get; }

		public CameraFace CameraFace { get; }

		public bool IsOpen => throw new NotImplementedException();

		public CameraDeviceImpl(string id)
		{
			Id = id;
		}

		public bool HasCapability(CameraCapability capability)
		{
			throw new NotImplementedException();
		}

		public Task<bool> Open()
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public ICameraSurface CreateSurface(int width, int height, CaptureType captureType)
		{
			throw new NotImplementedException();
		}

		public Task<bool> Start()
		{
			throw new NotImplementedException();
		}

		public List<CameraSize> GetOutputSizes()
		{
			throw new NotImplementedException();
		}

		public List<CameraSize> GetPreviewSizes()
		{
			throw new NotImplementedException();
		}

		public void Close()
		{
			throw new NotImplementedException();
		}
	}
}
