using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

using WindowsClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace Microsoft.Maui.ApplicationModel.DataTransfer
{
	public partial class ClipboardImplementation : IClipboard
	{
		public Task SetTextAsync(string text)
		{
			var dataPackage = new DataPackage();
			dataPackage.SetText(text);
			WindowsClipboard.SetContent(dataPackage);
			return Task.CompletedTask;
		}

		public bool HasText
			=> WindowsClipboard.GetContent().Contains(StandardDataFormats.Text);

		public Task<string> GetTextAsync()
		{
			var clipboardContent = WindowsClipboard.GetContent();
			return clipboardContent.Contains(StandardDataFormats.Text)
				? clipboardContent.GetTextAsync().AsTask()
				: Task.FromResult<string>(null);
		}

		public void StartClipboardListeners()
			=> WindowsClipboard.ContentChanged += ClipboardChangedEventListener;

		public void StopClipboardListeners()
			=> WindowsClipboard.ContentChanged -= ClipboardChangedEventListener;

		public void ClipboardChangedEventListener(object sender, object val) => Clipboard.ClipboardChangedInternal();
	}
}
