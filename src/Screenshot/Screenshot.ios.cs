﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Essentials
{
	public static partial class Screenshot
	{
		[DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		static extern bool bool_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

		[DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);


		static bool PlatformIsCaptureSupported =>
			true;

		static Task<ScreenshotResult> PlatformCaptureAsync()
		{
			var scenes = UIApplication.SharedApplication.ConnectedScenes;
			var currentScene = scenes.ToArray().Where(n => n.ActivationState == UISceneActivationState.ForegroundActive).FirstOrDefault();
			if (currentScene == null)
				throw new InvalidOperationException("Unable to find current scene.");

			var uiWindowScene = currentScene as UIWindowScene;
			if (uiWindowScene == null)
				throw new InvalidOperationException("Unable to find current uiwindow scene.");

			var currentWindow = uiWindowScene.Windows.FirstOrDefault(n => n.IsKeyWindow);
			if (currentWindow == null)
				throw new InvalidOperationException("Unable to find current window.");

			var image = currentWindow.Render(currentWindow.Layer, UIScreen.MainScreen.Scale);
			var result = new ScreenshotResult(image);

			return Task.FromResult(result);
		}

		public static byte[] RenderAsPng(this UIWindow window, object obj, nfloat scale, bool skipChildren = true)
		{
			using (var image = Render(window, obj, scale, skipChildren))
				return image != null ? image.RenderAsPng() : null;
		}

		public static byte[] RenderAsJpeg(this UIWindow window, object obj, nfloat scale, bool skipChildren = true)
		{
			using (var image = Render(window, obj, scale, skipChildren))
				return image != null ? image.RenderAsJpeg() : null;
		}

		public static byte[] RenderAsPng(this UIImage image) => image.AsPNG().AsImageBytes();

		public static byte[] RenderAsJpeg(this UIImage image) => image.AsJPEG().AsImageBytes();

		static byte[] AsImageBytes(this NSData data)
		{
			try
			{
				var result = new byte[data.Length];
				Marshal.Copy(data.Bytes, result, 0, (int)data.Length);
				return result;
			}
			finally
			{
				data.Dispose();
			}
		}

		public static UIImage Render(this UIWindow window, object obj, nfloat scale, bool skipChildren = true)
		{
			CGContext ctx = null;
			Exception error = null;

			var viewController = obj as UIViewController;
			if (viewController != null)
			{
				// NOTE: We rely on the window frame having been set to the correct size when this method is invoked.
				UIGraphics.BeginImageContextWithOptions(window.Bounds.Size, false, scale);
				ctx = UIGraphics.GetCurrentContext();

				if (!TryRender(window, ctx, ref error))
				{
					//FIXME: test/handle this case
				}

				// Render the status bar with the correct frame size
				UIApplication.SharedApplication.TryHideStatusClockView();
				var statusbarWindow = UIApplication.SharedApplication.GetStatusBarWindow();
				if (statusbarWindow != null/* && metrics.StatusBar != null*/)
				{
					statusbarWindow.Frame = window.Frame;
					statusbarWindow.Layer.RenderInContext(ctx);
				}
			}

			var view = obj as UIView;
			if (view != null)
			{
				UIGraphics.BeginImageContextWithOptions(view.Bounds.Size, false, scale);
				ctx = UIGraphics.GetCurrentContext();
				// ctx will be null if the width/height of the view is zero
				if (ctx != null)
					TryRender(view, ctx, ref error);
			}

			var layer = obj as CALayer;
			if (layer != null)
			{
				UIGraphics.BeginImageContextWithOptions(layer.Bounds.Size, false, scale);
				ctx = UIGraphics.GetCurrentContext();
				if (ctx != null)
					TryRender(layer, ctx, skipChildren, ref error);
			}

			if (ctx == null)
				return null;

			var image = UIGraphics.GetImageFromCurrentImageContext();
			UIGraphics.EndImageContext();
			return image;
		}

		static bool TryRender(UIView view, CGContext ctx, ref Exception error)
		{
			try
			{
				view.DrawViewHierarchy(view.Bounds, afterScreenUpdates: true);
				return true;
			}
			catch (Exception e)
			{
				error = e;
				return false;
			}
		}

		static bool TryRender(CALayer layer, CGContext ctx, bool skipChildren, ref Exception error)
		{
			try
			{
				Dictionary<IntPtr, bool> visibilitySnapshot = null;
				if (skipChildren)
					visibilitySnapshot = GetVisibilitySnapshotAndHideLayers(layer);
				layer.RenderInContext(ctx);
				if (skipChildren)
					ResetLayerVisibilitiesFromSnapshot(layer, visibilitySnapshot);
				return true;
			}
			catch (Exception e)
			{
				error = e;
				return false;
			}
		}

		static Dictionary<IntPtr, bool> GetVisibilitySnapshotAndHideLayers(CALayer layer)
		{
			var visibilitySnapshot = new Dictionary<IntPtr, bool>();
			if (layer.Sublayers == null)
				return visibilitySnapshot;
			foreach (var sublayer in layer.Sublayers)
			{
				var subSnapshot = GetVisibilitySnapshotAndHideLayers(sublayer);
				foreach (var kvp in subSnapshot)
					visibilitySnapshot.Add(kvp.Key, kvp.Value);
				visibilitySnapshot.Add(sublayer.Handle, sublayer.Hidden);
				sublayer.Hidden = true;
			}
			return visibilitySnapshot;
		}

		static void ResetLayerVisibilitiesFromSnapshot(
			CALayer layer,
			Dictionary<IntPtr, bool> visibilitySnapshot)
		{
			if (layer.Sublayers == null)
				return;
			foreach (var sublayer in layer.Sublayers)
			{
				ResetLayerVisibilitiesFromSnapshot(sublayer, visibilitySnapshot);
				if (visibilitySnapshot != null)
					sublayer.Hidden = visibilitySnapshot[sublayer.Handle];
			}
		}

		static void TryHideStatusClockView(this UIApplication app)
		{
			var statusBarWindow = app.GetStatusBarWindow();
			if (statusBarWindow == null)
				return;

			var clockView = statusBarWindow.FindSubview(
				"UIStatusBar",
				"UIStatusBarForegroundView",
				"UIStatusBarTimeItemView"
			);

			if (clockView != null)
				clockView.Hidden = true;
		}

		static UIWindow GetStatusBarWindow(this UIApplication app)
		{
			if (!bool_objc_msgSend_IntPtr(app.Handle,
				Selectors.respondsToSelector.Handle,
				Selectors.statusBarWindow.Handle))
				return null;

			var ptr = IntPtr_objc_msgSend(app.Handle, Selectors.statusBarWindow.Handle);
			return ptr != IntPtr.Zero ? ObjCRuntime.Runtime.GetNSObject(ptr) as UIWindow : null;
		}

		static class Selectors
		{
			public static readonly Selector statusBarWindow = new Selector("statusBarWindow");
			public static readonly Selector respondsToSelector = new Selector("respondsToSelector:");
		}

		static UIView FindSubview(this UIView view, params string[] classNames)
		{
			return FindSubview(view, ((IEnumerable<string>)classNames).GetEnumerator());
		}

		static UIView FindSubview(UIView view, IEnumerator<string> classNames)
		{
			if (!classNames.MoveNext())
				return view;

			foreach (var subview in view.Subviews)
			{
				if (subview.ToString().StartsWith("<" + classNames.Current + ":", StringComparison.Ordinal))
					return FindSubview(subview, classNames);
			}

			return null;
		}
	}

	public partial class ScreenshotResult
	{
		readonly UIImage bmp;

		internal ScreenshotResult(UIImage bmp)
			: base()
		{
			this.bmp = bmp;

			Width = (int)bmp.Size.Width;
			Height = (int)bmp.Size.Height;
		}

		internal Task<Stream> PlatformOpenReadAsync(ScreenshotFormat format)
		{
			var data = format == ScreenshotFormat.Png ? bmp.RenderAsPng() : bmp.RenderAsJpeg();
			return Task.FromResult(new MemoryStream(data) as Stream);
		}
	}
}
