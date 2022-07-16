using System;
using System.Globalization;
using Windows.ApplicationModel;
using System.Diagnostics;
using System.Reflection;
#if WINDOWS
using Microsoft.UI.Xaml;
#else
using Windows.UI.Xaml;
#endif

namespace Microsoft.Maui.ApplicationModel
{
	class AppInfoImplementation : IAppInfo
	{
		static readonly Assembly _launchingAssembly = Assembly.GetEntryAssembly();

		const string SettingsUri = "ms-settings:appsfeatures-app";

		ApplicationTheme? _applicationTheme;

		public AppInfoImplementation()
		{
			// TODO: NET7 use new public events
			if (WindowStateManager.Default is WindowStateManagerImplementation impl)
				impl.ActiveWindowThemeChanged += OnActiveWindowThemeChanged;

			if (MainThread.IsMainThread)
				OnActiveWindowThemeChanged();
		}

		public string PackageName => AppInfoUtils.IsPackagedApp
			? Package.Current.Id.Name
			: _launchingAssembly.GetAppInfoValue("PackageName") ?? _launchingAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? string.Empty;

		// TODO: NET7 add this as a actual data point and public property if it is valid on platforms
		internal static string PublisherName => AppInfoUtils.IsPackagedApp
			? Package.Current.PublisherDisplayName
			: _launchingAssembly.GetAppInfoValue("PublisherName") ?? _launchingAssembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;

		public string Name => AppInfoUtils.IsPackagedApp
			? Package.Current.DisplayName
			: _launchingAssembly.GetAppInfoValue("Name") ?? _launchingAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? string.Empty;

		public Version Version => AppInfoUtils.IsPackagedApp
			? Package.Current.Id.Version.ToVersion()
			: _launchingAssembly.GetAppInfoVersionValue("Version") ?? _launchingAssembly.GetName().Version;

		public string VersionString => Version.ToString();

		public string BuildString => Version.Revision.ToString(CultureInfo.InvariantCulture);

		public void ShowSettingsUI()
		{
			if (AppInfoUtils.IsPackagedApp)
				global::Windows.System.Launcher.LaunchUriAsync(new Uri(SettingsUri)).WatchForError();
			else
				Process.Start(new ProcessStartInfo { FileName = SettingsUri, UseShellExecute = true });
		}

		public AppTheme RequestedTheme
		{
			get
			{
				if (MainThread.IsMainThread && Application.Current != null)
					_applicationTheme = Application.Current.RequestedTheme;
				else if (_applicationTheme == null)
					return AppTheme.Unspecified;

				return _applicationTheme == ApplicationTheme.Dark ? AppTheme.Dark : AppTheme.Light;
			}
		}

		public AppPackagingModel PackagingModel => AppInfoUtils.IsPackagedApp
			? AppPackagingModel.Packaged
			: AppPackagingModel.Unpackaged;

		public LayoutDirection RequestedLayoutDirection =>
			CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? LayoutDirection.RightToLeft : LayoutDirection.LeftToRight;

		void OnActiveWindowThemeChanged(object sender = null, EventArgs e = null)
		{
			if (Application.Current is Application app)
				_applicationTheme = app.RequestedTheme;
		}
	}

	static class AppInfoUtils
	{
		static readonly Lazy<bool> _isPackagedAppLazy = new Lazy<bool>(() =>
		{
			try
			{
				if (Package.Current != null)
					return true;
			}
			catch
			{
				// no-op
			}

			return false;
		});

		public static bool IsPackagedApp => _isPackagedAppLazy.Value;

		public static Version ToVersion(this PackageVersion version) =>
			new Version(version.Major, version.Minor, version.Build, version.Revision);

		public static Version GetAppInfoVersionValue(this Assembly assembly, string name)
		{
			if (assembly.GetAppInfoValue(name) is string value && !string.IsNullOrEmpty(value))
				return Version.Parse(value);

			return null;
		}

		public static string GetAppInfoValue(this Assembly assembly, string name) =>
			assembly.GetMetadataAttributeValue("Microsoft.Maui.ApplicationModel.AppInfo." + name);

		public static string GetMetadataAttributeValue(this Assembly assembly, string key)
		{
			foreach (var attr in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
			{
				if (attr.Key == key)
					return attr.Value;
			}

			return null;
		}
	}
}
