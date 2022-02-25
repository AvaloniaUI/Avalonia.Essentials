using System;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using MobileCoreServices;
using ObjCRuntime;
using Photos;
using UIKit;

namespace Microsoft.Maui.Essentials.Implementations
{
	public partial class MediaPickerImplementation : IMediaPicker
	{
		static UIImagePickerController picker;

		public bool IsCaptureSupported
			=> UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera);

		public Task<FileResult> PickPhotoAsync(MediaPickerOptions options)
			=> PhotoAsync(options, true, true);

		public Task<FileResult> CapturePhotoAsync(MediaPickerOptions options)
			=> PhotoAsync(options, true, false);

		public Task<FileResult> PickVideoAsync(MediaPickerOptions options)
			=> PhotoAsync(options, false, true);

		public Task<FileResult> CaptureVideoAsync(MediaPickerOptions options)
			=> PhotoAsync(options, false, false);

		public async Task<FileResult> PhotoAsync(MediaPickerOptions options, bool photo, bool pickExisting)
		{
			var sourceType = pickExisting ? UIImagePickerControllerSourceType.PhotoLibrary : UIImagePickerControllerSourceType.Camera;
			var mediaType = photo ? UTType.Image : UTType.Movie;

			if (!UIImagePickerController.IsSourceTypeAvailable(sourceType))
				throw new FeatureNotSupportedException();
			if (!UIImagePickerController.AvailableMediaTypes(sourceType).Contains(mediaType))
				throw new FeatureNotSupportedException();

			if (!photo && !pickExisting)
				await Permissions.EnsureGrantedAsync<Permissions.Microphone>();

			// Check if picking existing or not and ensure permission accordingly as they can be set independently from each other
			if (pickExisting && !OperatingSystem.IsIOSVersionAtLeast(11, 0))
				await Permissions.EnsureGrantedAsync<Permissions.Photos>();

			if (!pickExisting)
				await Permissions.EnsureGrantedAsync<Permissions.Camera>();

			var vc = Platform.GetCurrentViewController(true);

			picker = new UIImagePickerController();
			picker.SourceType = sourceType;
			picker.MediaTypes = new string[] { mediaType };
			picker.AllowsEditing = false;
			if (!photo && !pickExisting)
				picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Video;

			if (!string.IsNullOrWhiteSpace(options?.Title))
				picker.Title = options.Title;

			if (DeviceInfo.Idiom == DeviceIdiom.Tablet && picker.PopoverPresentationController != null && vc.View != null)
				picker.PopoverPresentationController.SourceRect = vc.View.Bounds;

			var tcs = new TaskCompletionSource<FileResult>(picker);
			picker.Delegate = new PhotoPickerDelegate
			{
				CompletedHandler = async info =>
				{
					GetFileResult(info, tcs);
					await vc.DismissViewControllerAsync(true);
				}
			};

			if (picker.PresentationController != null)
			{
				picker.PresentationController.Delegate =
					new Platform.UIPresentationControllerDelegate(() => GetFileResult(null, tcs));
			}

			await vc.PresentViewControllerAsync(picker, true);

			var result = await tcs.Task;

			picker?.Dispose();
			picker = null;

			return result;
		}

		static void GetFileResult(NSDictionary info, TaskCompletionSource<FileResult> tcs)
		{
			try
			{
				tcs.TrySetResult(DictionaryToMediaFile(info));
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		}

		static FileResult DictionaryToMediaFile(NSDictionary info)
		{
			if (info == null)
				return null;

			PHAsset phAsset = null;
			NSUrl assetUrl = null;

			if (OperatingSystem.IsIOSVersionAtLeast(11, 0))
			{
				assetUrl = info[UIImagePickerController.ImageUrl] as NSUrl;

				// Try the MediaURL sometimes used for videos
				if (assetUrl == null)
					assetUrl = info[UIImagePickerController.MediaURL] as NSUrl;

				if (assetUrl != null)
				{
					if (!assetUrl.Scheme.Equals("assets-library", StringComparison.InvariantCultureIgnoreCase))
						return new UIDocumentFileResult(assetUrl);

					phAsset = info.ValueForKey(UIImagePickerController.PHAsset) as PHAsset;
				}
			}

#if !MACCATALYST
			if (phAsset == null)
			{
				assetUrl = info[UIImagePickerController.ReferenceUrl] as NSUrl;

				if (assetUrl != null)
					phAsset = PHAsset.FetchAssets(new NSUrl[] { assetUrl }, null)?.LastObject as PHAsset;
			}
#endif

			if (phAsset == null || assetUrl == null)
			{
				var img = info.ValueForKey(UIImagePickerController.OriginalImage) as UIImage;

				if (img != null)
					return new UIImageFileResult(img);
			}

			if (phAsset == null || assetUrl == null)
				return null;

			string originalFilename = PHAssetResource.GetAssetResources(phAsset).FirstOrDefault()?.OriginalFilename;
			return new PHAssetFileResult(assetUrl, phAsset, originalFilename);
		}

		class PhotoPickerDelegate : UIImagePickerControllerDelegate
		{
			public Action<NSDictionary> CompletedHandler { get; set; }

			public override void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info) =>
				CompletedHandler?.Invoke(info);

			public override void Canceled(UIImagePickerController picker) =>
				CompletedHandler?.Invoke(null);
		}
	}
}
