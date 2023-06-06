using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Hardware.Lights;
using Android.Media;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Grafika.GLES;
using Java.Nio;
using static Android.Icu.Text.ListFormatter;
using static Android.Telephony.CarrierConfigManager;

namespace Microsoft.Maui.CameraDevice
{
	public partial class CameraProviderImplementation : ICameraProvider
	{
		internal static CameraManager _cameraManager;

		public static CameraManager CameraManager => _cameraManager ??= Application.Context.GetSystemService(Context.CameraService) as CameraManager;

		public IEnumerable<ICameraDevice> GetAvailableDevices()
		{
			var cameraIds = CameraManager.GetCameraIdList();
			for (var i = 0; i < cameraIds.Length; i++)
			{
				var cameraId = cameraIds[i];
				yield return new CameraDeviceImpl(cameraId);
			}
		}
	}

	public class CameraDeviceImpl : Android.Hardware.Camera2.CameraDevice.StateCallback, ICameraDevice
	{
		public string Id { get; }

		private CameraCharacteristics _characteristics;
		private StreamConfigurationMap _configurationMap;
		private int[] _capabilities;

		private CameraCaptureSession? _session;
		private CaptureSession _captureSession;
		private Android.Hardware.Camera2.CameraDevice? _camera;
		private Task<bool> _waitingTask;
		private ManualResetEvent _waitEvent;
		private List<CameraSurface> _surfaces;
		private CaptureSessionState _sessionState;
		private CaptureRequest.Builder _repeatingRequest;

		private NativeThread _thread;
		private EglCore _context;
		private EGLSurface _eglSurface;
		private Handler _handler;

		public CameraFace CameraFace { get; }

		public bool IsOpen => _camera != null;

		public CameraDeviceImpl(string id)
		{
			Id = id;
			_characteristics = CameraProviderImplementation.CameraManager.GetCameraCharacteristics(Id);
			_configurationMap = _characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap) as StreamConfigurationMap;
			_capabilities = (int[])_characteristics.Get(CameraCharacteristics.RequestAvailableCapabilities);
			CameraFace = (CameraFace)(int)_characteristics.Get(CameraCharacteristics.LensFacing);
			_surfaces = new List<CameraSurface>();
		}

		public Task<bool> Open()
		{
			if(_waitingTask != null)
				throw new InvalidOperationException("Device Busy");

			if (_session != null)
				throw new InvalidOperationException("Camera is already opened");

			_waitEvent = new ManualResetEvent(false);

			var waitEvent = new ManualResetEvent(false);

			_thread = new NativeThread(() =>
			{
				_context = new EglCore(null, EglCore.FLAG_RECORDABLE);
				_eglSurface = _context.createOffscreenSurface(1, 1);
				_context.makeNothingCurrent();

				waitEvent.Set();
			});

			_thread.Start();

			waitEvent.WaitOne();

			_handler = new Handler(_thread.Looper);

			waitEvent?.Dispose();

			_waitEvent.Reset();
			_waitingTask = Task<bool>.Run(()=>
			{
				_waitEvent.WaitOne();
				return _camera != null;
			}); 
			CameraProviderImplementation.CameraManager.OpenCamera(Id, this, _handler);
			return _waitingTask;
		}

		public void Close()
		{
			(this as IDisposable).Dispose();
		}

		public bool HasCapability(CameraCapability capability)
		{
			throw new NotImplementedException();
		}

		void IDisposable.Dispose()
		{
			_session?.StopRepeating();
			_session?.Close();
			_session?.Dispose();
			_session = null;

			_camera?.Close();
			_camera?.Dispose();
			_camera = null;

			_surfaces.Clear();
			_waitEvent?.Set();
			_waitEvent?.Dispose();
			_waitEvent = null;

			_handler.Post(() =>
			{
				lock (_context)
				{
					_context?.makeNothingCurrent();
					_context.releaseSurface(_eglSurface);
					_context.release();
					_eglSurface?.Dispose();
					_context?.Dispose();
					_context = null;
					_thread?.Quit();
				}
			});
		}

		public ICameraSurface CreateSurface(int width, int height, CaptureType captureType)
		{
			if (!IsOpen)
				throw new InvalidOperationException("Camera is not open.");

			if(_session != null)
				throw new InvalidOperationException("Camera already has an active session. No new Surfaces can be created.");

			var surface = new CameraSurface(width, height, _thread.Looper, _context, _characteristics, captureType);

			_surfaces.Add(surface);

			return surface;
		}

		public Task<bool> Start()
		{
			if (_waitingTask != null)
				throw new InvalidOperationException("Device Busy");

			if (_session != null)
				throw new InvalidOperationException("Camera already has an active session.");

			var surfaces = _surfaces.Select(x => x.Surface);

			if(surfaces.Count() == 0)
				throw new InvalidOperationException("No surfaces have been created.");

			_waitEvent.Reset();
			_waitingTask = Task<bool>.Run(() =>
			{
				_waitEvent.WaitOne();
				return _session != null;
			});

			_sessionState = new CaptureSessionState();
			_sessionState.CaptureSessionStateChanged += SessionState_CaptureSessionStateChanged;

			if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
			{
				var output = surfaces.Select(s => new OutputConfiguration(s));

				var sessionConfig = new SessionConfiguration((int)SessionType.Regular,
					output.ToList(),
					AndroidX.Core.OS.ExecutorCompat.Create(_handler),
					_sessionState);
				_camera.CreateCaptureSession(sessionConfig);
			}
			else
			{
				_camera.CreateCaptureSession(surfaces.ToList(), _sessionState, _handler);
			}
			return _waitingTask;
		}

		private void SessionState_CaptureSessionStateChanged(object sender, EventArgs e)
		{
			_sessionState.CaptureSessionStateChanged -= SessionState_CaptureSessionStateChanged;

			if (_sessionState.IsConfigured)
			{
				_session = _sessionState.Session;

				_captureSession = new CaptureSession(_session!, _handler);

				foreach(var surface in _surfaces)
				{
					surface.CaptureSession = _captureSession;
				}
			}

			_waitEvent?.Set();
			_waitingTask = null;
		}

		internal static int GetRotation(CameraCharacteristics characteristics)
		{
			var windowManager = Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			var displayOrientation = windowManager.DefaultDisplay?.Rotation;
			var sensorOrientation = (int)characteristics.Get(CameraCharacteristics.SensorOrientation);

			var displayAngle = displayOrientation switch
			{
				SurfaceOrientation.Rotation0 => 90,
				SurfaceOrientation.Rotation90 => 0,
				SurfaceOrientation.Rotation180 => 270,
				SurfaceOrientation.Rotation270 => 180,
				_ => throw new NotImplementedException(),
			};

			return  (displayAngle + sensorOrientation + 180) % 360;
		}

		public override void OnDisconnected(Android.Hardware.Camera2.CameraDevice camera)
		{
			_waitEvent?.Set();
			_waitingTask = null;
		}

		public override void OnError(Android.Hardware.Camera2.CameraDevice camera, [GeneratedEnum] Android.Hardware.Camera2.CameraError error)
		{
			_waitEvent?.Set();
			_waitingTask = null;
		}

		public override void OnOpened(Android.Hardware.Camera2.CameraDevice camera)
		{
			_camera = camera;
			_waitEvent?.Set();
			_waitingTask = null;
		}

		public List<CameraSize> GetOutputSizes()
		{
			var cameraSizes = _configurationMap.GetOutputSizes((int)ImageFormatType.Jpeg);

			return cameraSizes.Select(x => new CameraSize(x.Width, x.Height)).Reverse().ToList();
		}

		public List<CameraSize> GetPreviewSizes()
		{
			var cameraSizes = _configurationMap.GetOutputSizes(Java.Lang.Class.FromType(typeof(SurfaceTexture)));

			return cameraSizes.Select(x => new CameraSize(x.Width, x.Height)).Reverse().ToList();
		}
	}

	internal class CaptureRequestCallback : CameraCaptureSession.CaptureCallback
	{
		readonly IEnumerable<CameraSurface> _cameraSurfaces;

		public CaptureRequestCallback(IEnumerable<CameraSurface> cameraSurfaces)
		{
			_cameraSurfaces = cameraSurfaces;
		}

		public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
		{
			base.OnCaptureCompleted(session, request, result);
		}
	}

	internal class CaptureSessionState : CameraCaptureSession.StateCallback
	{
		public event EventHandler<EventArgs> CaptureSessionStateChanged;

		internal CameraCaptureSession Session { get; private set; }
		internal bool IsConfigured { get; private set; }

		public override void OnConfigured(CameraCaptureSession session)
		{
			IsConfigured = true;
			Session = session;
			CaptureSessionStateChanged?.Invoke(this, new EventArgs());
		}

		public override void OnConfigureFailed(CameraCaptureSession session)
		{
			Session = session;
			CaptureSessionStateChanged?.Invoke(this, new EventArgs());
		}
	}

	public class CameraSurface : Java.Lang.Object, SurfaceTexture.IOnFrameAvailableListener, ImageReader.IOnImageAvailableListener, ICameraSurface
	{
		public event EventHandler<ImageCaptureEventArgs> OnImageCapturedEvent;

		private ImageReader _imageReader;

		internal Surface Surface { get; private set; }

		public int Width { get; }
		public int Height { get; }
		public CaptureType CaptureType { get => _captureType; set => _captureType = value; }

		public CaptureSession CaptureSession
		{
			get
			{
				return _captureSession;
			}
			set => _captureSession = value;
		}

		private Handler _handler;
		private FullFrameRect _frame;
		private OffscreenSurface _offScreenSurface;
		private int _texture;
		private SurfaceTexture _surfaceTexture;
		private CaptureType _captureType;
		internal int _focusX;
		internal int _focusY;
		internal bool _autoFocus = true;
		EglCore _context;
		readonly CameraCharacteristics _cameraCharacteristics;
		private CaptureSession _captureSession;

		public CameraSurface(int width, int height, Looper looper, EglCore context, CameraCharacteristics cameraCharacteristics, CaptureType captureType)
		{
			Width = width;
			Height = height;
			_context = context;
			CaptureType = captureType;
			_cameraCharacteristics = cameraCharacteristics;
			var waitEvent = new ManualResetEvent(false);

			if (captureType == CaptureType.StaticCapture)
			{
				_imageReader = ImageReader.NewInstance(width, height, ImageFormatType.Jpeg, 2);
				_imageReader.SetOnImageAvailableListener(this, null);
				Surface = _imageReader.Surface;
			}
			else
			{
				var windowManager = Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
				var displayOrientation = windowManager.DefaultDisplay?.Rotation;

				if (displayOrientation == SurfaceOrientation.Rotation0 || displayOrientation == SurfaceOrientation.Rotation180)
				{
					Width = height;
					Height = width;
				}
				_handler = new Handler(looper);
				_handler.Post(() =>
				{
					_offScreenSurface = new OffscreenSurface(context, Width, Height);
					_offScreenSurface.makeCurrent();
					var program = new Texture2dProgram(Texture2dProgram.ProgramType.TEXTURE_EXT);
					_frame = new FullFrameRect(program);

					_offScreenSurface.makeCurrent();
					_texture = _frame.createTextureObject();
					_surfaceTexture = new SurfaceTexture(_texture);
					_surfaceTexture.SetDefaultBufferSize(width, height);
					_surfaceTexture.SetOnFrameAvailableListener(this);

					Surface = new Surface(_surfaceTexture);

					_context.makeNothingCurrent();

					waitEvent.Set();
				});

				waitEvent.WaitOne();
				waitEvent.Dispose();
			}
		}

		void IDisposable.Dispose()
		{
			StopCapture();
			_imageReader?.Dispose();
			_imageReader = null;
			lock (_context)
			{
				_offScreenSurface?.makeCurrent();
				Surface?.Dispose();
				Surface = null;
				_surfaceTexture?.Dispose();
				_surfaceTexture = null;
				_frame?.release(true);
				_context.makeNothingCurrent();
				_context = null;
				_offScreenSurface?.releaseEglSurface();
				_offScreenSurface = null;
				_imageReader?.Dispose();
				_imageReader = null;
			}
		}

		public void OnImageAvailable(ImageReader reader)
		{
			var image = reader.AcquireLatestImage();
			if(image != null)
			{
				int offset = 0;
				var planes = image.GetPlanes();
				var buff = new byte[planes.Sum( x => x.Buffer.Remaining())];

				foreach (var plane in planes)
				{
					var buffer = plane.Buffer;
					var rem = buffer.Remaining();
					buffer.Get(buff, offset, rem);

					offset += rem;
				}

				Bitmap bmp = BitmapFactory.DecodeByteArray(buff, 0, buff.Length);
				var matrix = new Android.Graphics.Matrix();

				var face = (CameraFace)(int)_cameraCharacteristics.Get(CameraCharacteristics.LensFacing);

				if (face == CameraFace.Front)
				{
					matrix.PostScale(-1, 1);
				}
				matrix.PostRotate(CameraDeviceImpl.GetRotation(_cameraCharacteristics) - 90);
				var rotated = Bitmap.CreateBitmap(bmp, 0, 0, bmp.Width, bmp.Height, matrix, true);

				using var mem = new MemoryStream();
				rotated.Compress(Bitmap.CompressFormat.Jpeg, 90, mem);

				OnImageCapturedEvent?.Invoke(this, new ImageCaptureEventArgs(mem.ToArray(), rotated.Width, rotated.Height, (int)rotated.GetBitmapInfo().Stride));

				image.Close();
				bmp.Dispose();
				rotated.Dispose();
			}
		}

		public void RaiseImageAvailable(ImageCaptureEventArgs e)
		{
			OnImageCapturedEvent?.Invoke(this, e);
		}

		public void OnFrameAvailable(SurfaceTexture surfaceTexture)
		{
			Bitmap bitmap = null;

			if (_context == null)
				return;

			lock (_context)
			{
				if (_offScreenSurface == null)
					return;

				_offScreenSurface.makeCurrent();
				var mat = new float[16];
				_surfaceTexture?.UpdateTexImage();
				_surfaceTexture.GetTransformMatrix(mat);

				switch (CameraDeviceImpl.GetRotation(_cameraCharacteristics))
				{
					case 90:
						Android.Opengl.Matrix.RotateM(mat, 0, 90, 0, 0, 1);
						Android.Opengl.Matrix.TranslateM(mat, 0, 0, -1, 0);
						break;
					case 180:
						Android.Opengl.Matrix.RotateM(mat, 0, 180, 0, 0, 1);
						Android.Opengl.Matrix.TranslateM(mat, 0, -1, -1, 0);
						break;
					case 270:
						Android.Opengl.Matrix.RotateM(mat, 0, 270, 0, 0, 1);
						Android.Opengl.Matrix.TranslateM(mat, 0, -1, 0, 0);
						break;
				}
				var sceneMatrix = GlUtil.IDENTITY_MATRIX;
				var matrix = new Android.Graphics.Matrix();
				matrix.SetValues(sceneMatrix);

				var face = (CameraFace)(int)_cameraCharacteristics.Get(CameraCharacteristics.LensFacing);

				if (face == CameraFace.Front)
				{
					matrix.PostScale(-1, 1);
				}
				else
				{
					matrix.PostScale(1, -1);
				}

				matrix.GetValues(sceneMatrix);
				_frame.drawFrame(_texture, mat, sceneMatrix);
				bitmap = _offScreenSurface.getBitmap();

				_context.makeNothingCurrent();
			}

			if (bitmap != null)
			{
				var buffer = ByteBuffer.Allocate(bitmap.ByteCount);

				bitmap.CopyPixelsToBuffer(buffer);
				buffer.Rewind();
				var buff = new byte[bitmap.ByteCount];
				buffer.Get(buff, 0, buff.Length);

				OnImageCapturedEvent?.Invoke(this, new ImageCaptureEventArgs(buff, Width, Height, (int)bitmap.GetBitmapInfo().Stride));

				bitmap.Dispose();
			}
		}

		public void StartCapture()
		{
			StartRequest();
		}

		public void FocusPoint(int x, int y)
		{
			_focusX = x;
			_focusY = y;
			_autoFocus = false;

			StopCapture();
			StartRequest();
		}

		public void SetAutoFocus(bool autoFocus)
		{
			_autoFocus = autoFocus;

			StopCapture();
			StartRequest();
		}

		private void StartRequest()
		{
			if (CaptureType == CaptureType.StaticCapture)
			{
				CaptureSession.CaptureSingle(this);
			}
			else
			{
				CaptureSession?.AddRepeating(this);
			}
		}

		public void StopCapture()
		{
			if (CaptureType != CaptureType.StaticCapture)
				CaptureSession?.RemoveRepeating(this);
		}
	}

	public class CaptureSession : CameraCaptureSession.CaptureCallback
	{
		readonly CameraCaptureSession _cameraCaptureSession;
		readonly Handler _handler;
		private List<CameraSurface> _repeatingSurfaces;
		private CaptureRequest.Builder _repeatingRequest;

		public CaptureSession(CameraCaptureSession cameraCaptureSession, Handler handler)
		{
			_cameraCaptureSession = cameraCaptureSession;
			_handler = handler;
			_repeatingSurfaces = new List<CameraSurface>();
		}

		internal void AddRepeating(CameraSurface surface)
		{
			if (_repeatingSurfaces.Contains(surface))
				return;

			_repeatingSurfaces.Add(surface);

			StartRepeating();
		}

		internal void RemoveRepeating(CameraSurface cameraSurface)
		{
			_repeatingSurfaces.Remove(cameraSurface);

			StopRepeating();
			StartRepeating();
		}

		private void StopRepeating()
		{
			if(_repeatingRequest != null)
			{
				_cameraCaptureSession.StopRepeating();

				_repeatingRequest.Dispose();
				_repeatingRequest = null;
			}
		}

		public void Focus(Rect rect)
		{

		}

		private void StartRepeating()
		{
			if (_repeatingSurfaces.Count == 0)
				return;

			var isPreview = !_repeatingSurfaces.Any(x => x.CaptureType == CaptureType.Record);

			if (_repeatingRequest == null)
			{
				_repeatingRequest = _cameraCaptureSession.Device.CreateCaptureRequest(isPreview ? CameraTemplate.Preview : CameraTemplate.Record);

				foreach (var surface in _repeatingSurfaces)
				{
					_repeatingRequest.AddTarget(surface.Surface);
				}
			}

			SetQuality(_repeatingRequest);
			_repeatingRequest.Set(CaptureRequest.NoiseReductionMode, (int)NoiseReductionMode.Fast);
			_repeatingRequest.Set(CaptureRequest.ControlCaptureIntent, isPreview ? (int)ControlCaptureIntent.Preview : (int)ControlCaptureIntent.VideoRecord);

			_cameraCaptureSession.SetRepeatingRequest(_repeatingRequest.Build(), this, _handler);
		}

		private void SetQuality(CaptureRequest.Builder request)
		{
			request.Set(CaptureRequest.ControlMode, (int)ControlMode.Auto);
			request.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);
			request.Set(CaptureRequest.ColorCorrectionMode, (int)ColorCorrectionMode.HighQuality);
			request.Set(CaptureRequest.LensOpticalStabilizationMode, (int)LensOpticalStabilizationMode.On);
		}

		internal void CaptureSingle(CameraSurface cameraSurface)
		{
			var singleRequest = _cameraCaptureSession.Device.CreateCaptureRequest(CameraTemplate.StillCapture);
			singleRequest.AddTarget(cameraSurface.Surface);

			SetQuality(singleRequest);
			singleRequest.Set(CaptureRequest.JpegQuality, (Java.Lang.Byte)(sbyte)100);
			singleRequest.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.On);
			singleRequest.Set(CaptureRequest.NoiseReductionMode, (int)NoiseReductionMode.HighQuality);
			singleRequest.Set(CaptureRequest.ControlCaptureIntent, (int)ControlCaptureIntent.StillCapture);

			_cameraCaptureSession.Capture(singleRequest.Build(), this, _handler);
		}
	}

	internal class NativeThread : Java.Lang.Thread
	{
		Looper _looper;
		private Action _init;

		public NativeThread(Action init)
		{
			_init = init;
		}

		public Looper Looper { get => _looper; set => _looper = value; }

		public override void Run()
		{
			Looper.Prepare();

			Looper = Looper.MyLooper();
			_init();

			Looper.Loop();
		}

		public void Quit()
		{
			_looper.QuitSafely();
			_looper.Dispose();
			Dispose();
		}
	}
}
