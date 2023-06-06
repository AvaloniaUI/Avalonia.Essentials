using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace Microsoft.Maui.CameraDevice
{
	public static class Camera
	{
		private static ICameraProvider defaultImplementation;

		public static ICameraProvider Default
			=> defaultImplementation ??= new CameraProviderImplementation();

		public static IEnumerable<ICameraDevice> GetAvailableDevices()
		{
			return Default.GetAvailableDevices();
		}
	}

	partial class CameraProviderImplementation : ICameraProvider
	{

	}


	public interface ICameraProvider
    {
		IEnumerable<ICameraDevice> GetAvailableDevices();
    }

	public interface ICameraDevice : IDisposable
	{
		string Id { get; }
		bool IsOpen { get; }
		CameraFace CameraFace {  get; }

		ICameraSurface CreateSurface(int width, int height, CaptureType captureType);
		bool HasCapability(CameraCapability capability);
		Task<bool> Open();
		Task<bool> Start();
		List<CameraSize> GetOutputSizes();
		List<CameraSize> GetPreviewSizes();
		void Close();
	}

	public interface ICameraSurface : IDisposable
	{
		int Width { get; }
		int Height { get; }

		event EventHandler<ImageCaptureEventArgs> OnImageCapturedEvent;

		void StartCapture();
		void StopCapture();
		void FocusPoint(int x, int y);
		void SetAutoFocus(bool autoFocus);
	}

	public enum CameraCapability
	{

	}

	public enum CaptureType
	{
		Preview,
		StaticCapture,
		Record
	}

	public enum CameraFace
	{
		Front,
		Back,
		External
	}

	public struct CameraSize
	{
		public int Width { get; }
		public int Height { get; }

		public override bool Equals(object obj)
		{
			return obj is CameraSize cameraSize && cameraSize.Width == Width && cameraSize.Height == Height;
		}

		public static bool operator==(CameraSize x, CameraSize y)
		{
			return x.Equals(y);
		}

		public static bool operator!=(CameraSize x, CameraSize y)
		{
			return !x.Equals(y);
		}

		public CameraSize(int width, int height)
		{
			Width = width;
			Height = height;
		}

		public override string ToString()
		{
			return $"{Width}x{Height}";
		}
	}

	public class ImageCaptureEventArgs : EventArgs
	{
		public ImageCaptureEventArgs(byte[] data, int width, int height, int stride)
		{
			Data = data;
			Width = width;
			Height = height;
			Stride = stride;
		}

		public byte[] Data { get; }
		public int Width { get; }
		public int Height { get; }

		public int Stride { get; }
	}
}
