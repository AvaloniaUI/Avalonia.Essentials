namespace Microsoft.Maui.Essentials;

/// <summary>
/// Rect struct.
/// </summary>
public record struct Rect(double X, double Y, double Width, double Height)
{
	/// <summary>
	/// Empty rect.
	/// </summary>
	public static Rect Zero => default;

#if IOS
	/// <summary>
	/// Convert to CGRect.
	/// </summary>
	public CoreGraphics.CGRect AsCGRect() => new(X, Y, Width, Height);
#endif
}