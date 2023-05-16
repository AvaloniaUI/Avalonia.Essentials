namespace Microsoft.Maui;

/// <summary>
/// Color struct.
/// </summary>
public record struct Color(float A, float R, float G, float B);

/// <summary>
/// Helpers.
/// </summary>
public static class ColorEx
{
#if IOS
	/// <summary>
	/// Convert to UIColor.
	/// </summary>
	public static UIKit.UIColor? AsUIColor(this Color? color) => color is { } c
		? new(c.R, c.G, c.B, c.A)
		: null;
#endif

	/// <summary>
	/// Convert to int.
	/// </summary>
	public static int ToInt(this Color? color) => color is { } c
		? ((byte)c.A * 255) << 24 | ((byte)c.R * 255) << 16 | ((byte)c.G * 255) << 8 | ((byte)c.B * 255)
		: 0;
}