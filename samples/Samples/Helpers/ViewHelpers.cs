
using Avalonia;
using Avalonia.VisualTree;

namespace Samples.Helpers
{
	public static class ViewHelpers
	{
		public static Microsoft.Maui.Essentials.Rect GetAbsoluteBounds(this Visual element)
		{
			var bounds = element.GetTransformedBounds().Value;
			var avRect = new Rect(bounds.Transform.Transform(bounds.Bounds.Position), bounds.Bounds.Size);
			return new Microsoft.Maui.Essentials.Rect(avRect.X, avRect.Y, avRect.Width, avRect.Height);
		}
	}
}
