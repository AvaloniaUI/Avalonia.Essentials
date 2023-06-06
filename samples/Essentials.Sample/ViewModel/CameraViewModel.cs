using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.CameraDevice;

namespace Samples.ViewModel
{
	public class CameraViewModel : BaseViewModel
	{
		private int _selectedCameraIndex = -1;
		private int _selectedCameraSizeIndex = -1;
		private int _selectedCameraPreviewSizeIndex = -1;
		private ICameraDevice _camera;
		private ICameraSurface _previewSurface;
		private WriteableBitmap _bitmap;
		private ICameraSurface _captureSurface;
		private ImageCaptureEventArgs _lastPreviewCapture;

		public Image? CameraPreviewer { get; set; }
		public AvaloniaList<ICameraDevice> Cameras { get; set; } = new AvaloniaList<ICameraDevice>();
		public AvaloniaList<CameraSize> SelectedCameraSizes { get; set; } = new AvaloniaList<CameraSize>();
		public AvaloniaList<CameraSize> SelectedCameraPreviewSizes { get; set; } = new AvaloniaList<CameraSize>();

		public int SelectedCameraIndex
		{
			get
			{
				return _selectedCameraIndex;
			}
			set
			{
				this.SetProperty(ref _selectedCameraIndex, value, nameof(SelectedCameraIndex), onChanged:UpdateCameraSizes);
			}
		}

		public int SelectedCameraSizeIndex
		{
			get
			{
				return _selectedCameraSizeIndex;
			}
			set
			{
				this.SetProperty(ref _selectedCameraSizeIndex, value, nameof(SelectedCameraSizeIndex));
			}
		}

		public int SelectedPreviewSizeIndex
		{
			get
			{
				return _selectedCameraPreviewSizeIndex;
			}
			set
			{
				this.SetProperty(ref _selectedCameraPreviewSizeIndex, value, nameof(SelectedPreviewSizeIndex));
			}
		}

		public CameraViewModel()
		{
		}

		public override void OnAppearing()
		{
			base.OnAppearing();
		}

		public override void OnDisappearing()
		{

			base.OnDisappearing();
		}

		public async void LoadCameras()
		{
			var permission = await new Permissions.Camera().RequestAsync();

			if (permission == PermissionStatus.Granted)
			{
				var cameras = Camera.GetAvailableDevices().ToList();

				Cameras.Clear();

				Cameras.AddRange(cameras);

				SelectedCameraIndex = cameras.Count > 0 ? 0 : -1;
			}
		}

		private void UpdateCameraSizes()
		{
			SelectedCameraSizeIndex = -1;

			SelectedCameraSizes.Clear();

			if (_selectedCameraIndex >= 0)
			{
				SelectedCameraSizes.AddRange(Cameras[_selectedCameraIndex].GetOutputSizes());

				SelectedCameraSizeIndex = SelectedCameraSizes.Count == 0 ? -1 : 0;

				SelectedCameraPreviewSizes.AddRange(Cameras[_selectedCameraIndex].GetPreviewSizes());

				SelectedPreviewSizeIndex = SelectedCameraPreviewSizes.Count == 0 ? -1 : 0;
			}
		}

		public async void StartSelectedCamera()
		{
			if (SelectedCameraIndex < 0 || SelectedPreviewSizeIndex < 0 || _camera != null)
				return;

			_camera = Cameras[SelectedCameraIndex];

			if (await _camera.Open())
			{
				var previewSize = SelectedCameraPreviewSizes[_selectedCameraPreviewSizeIndex];
				var captureSize = SelectedCameraSizes[SelectedCameraSizeIndex];

				_previewSurface = _camera.CreateSurface(previewSize.Width, previewSize.Height, CaptureType.Preview);
				_previewSurface.OnImageCapturedEvent += PreviewSurface_OnImageCapturedEvent;
				_captureSurface = _camera.CreateSurface(captureSize.Width, captureSize.Height, CaptureType.StaticCapture);
				_captureSurface.OnImageCapturedEvent += CaptureSurface_OnImageCapturedEvent;
				var started = await _camera.Start();
				_previewSurface.StartCapture();
				CameraPreviewer.InvalidateMeasure();
			}
		}

		private async void CaptureSurface_OnImageCapturedEvent(object sender, ImageCaptureEventArgs e)
		{
			await SaveCaptureToDisk(e.Width, e.Height, e.Data, "jpeg");
		}

		private async Task SaveCaptureToDisk(int width, int height, byte[] data, string extension)
		{
			var name = $"capture-{width}x{height}.{extension}";

			var storageProvider = TopLevel.GetTopLevel(CameraPreviewer)?.StorageProvider;

			if (storageProvider != null)
			{
				var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
				{
					SuggestedFileName = name,
					ShowOverwritePrompt = true
				});

				if (file != null)
				{
					var stream = await file.OpenWriteAsync();

					stream?.Write(data, 0, data.Length);
				}
			}
		}

		public void StopCamera()
		{
			if(_camera != null)
			{
				_previewSurface?.StopCapture();
				_previewSurface?.Dispose();
				_camera?.Dispose();
				CameraPreviewer.InvalidateMeasure();

				_camera = null;
			}
		}

		public async void SavePreviewCapture()
		{
			if (_lastPreviewCapture != null)
			{
				using var bitmap = GetBitmap(_lastPreviewCapture);
				using var mem = new MemoryStream();

				bitmap.Save(mem);

				await SaveCaptureToDisk(_lastPreviewCapture.Width, _lastPreviewCapture.Height, mem.ToArray(), "png");
			}
		}

		public void SaveCapture()
		{
			_captureSurface?.StartCapture();
		}

		private unsafe IBitmap GetBitmap(ImageCaptureEventArgs e)
		{
			var rgb = _lastPreviewCapture.Data;
			var captureBounds = new Rect(new Size(_lastPreviewCapture.Width, _lastPreviewCapture.Height));

			IBitmap bitmap = null;
			fixed (byte* rgbPtr = &rgb[0])
			{
				bitmap =
					new WriteableBitmap(PixelFormat.Rgba8888,
					  AlphaFormat.Opaque,
					  (nint)rgbPtr,
					  new Avalonia.PixelSize((int)captureBounds.Width, (int)captureBounds.Height),
					  new Avalonia.Vector(96, 96),
					  _lastPreviewCapture.Stride);
			}

			return bitmap;
		}

		private unsafe void PreviewSurface_OnImageCapturedEvent(object sender, ImageCaptureEventArgs e)
		{
			_lastPreviewCapture = e;
			Dispatcher.UIThread.InvokeAsync(() =>
			{
				_bitmap?.Dispose();

				_bitmap = (WriteableBitmap)GetBitmap(e);

				CameraPreviewer.Source = _bitmap;
			}).Wait();
		}
	}
}
