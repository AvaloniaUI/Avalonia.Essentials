This repository is archived.
The only reason for Avalonia.Essentials was to enable Maui.Essential working with any non-MAUI project.
But starting with .NET 8 Maui.Essentials can be used detached from the Maui, which is handy with Avalonia.

Alternatively, you can still integrate full Maui, if you wish, with [Avalonia.Maui](https://github.com/AvaloniaUI/AvaloniaMauiHybrid)https://github.com/AvaloniaUI/AvaloniaMauiHybrid hybrid project.

Keep in mind, Maui.Essentials still has a limited set of supported platforms. For example, it doesn't support Browser, Linux, macOS (only macCatalyst). But it still is really useful with mobile.

If you just need to call platform APIs, but don't want to use Essentials, please visit our documentation: 
https://docs.avaloniaui.net/docs/guides/platforms/platform-specific-code/dotnet
