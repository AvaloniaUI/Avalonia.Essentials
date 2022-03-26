#nullable enable
using System;

namespace Microsoft.Maui.ApplicationModel
{
	public interface IAppInfo
	{
		string PackageName { get; }

		string Name { get; }

		string VersionString { get; }

		Version Version { get; }

		string BuildString { get; }

		void ShowSettingsUI();

		AppTheme RequestedTheme { get; }

		AppPackagingModel PackagingModel { get; }

		LayoutDirection RequestedLayoutDirection { get; }
	}

	/// <include file="../../docs/Microsoft.Maui.Essentials/AppInfo.xml" path="Type[@FullName='Microsoft.Maui.Essentials.AppInfo']/Docs" />
	public static class AppInfo
	{
		/// <include file="../../docs/Microsoft.Maui.Essentials/AppInfo.xml" path="//Member[@MemberName='PackageName']/Docs" />
		public static string PackageName => Current.PackageName;

		/// <include file="../../docs/Microsoft.Maui.Essentials/AppInfo.xml" path="//Member[@MemberName='Name']/Docs" />
		public static string Name => Current.Name;

		/// <include file="../../docs/Microsoft.Maui.Essentials/AppInfo.xml" path="//Member[@MemberName='VersionString']/Docs" />
		public static string VersionString => Current.VersionString;

		/// <include file="../../docs/Microsoft.Maui.Essentials/AppInfo.xml" path="//Member[@MemberName='Version']/Docs" />
		public static Version Version => Current.Version;

		/// <include file="../../docs/Microsoft.Maui.Essentials/AppInfo.xml" path="//Member[@MemberName='BuildString']/Docs" />
		public static string BuildString => Current.BuildString;

		/// <include file="../../docs/Microsoft.Maui.Essentials/AppInfo.xml" path="//Member[@MemberName='ShowSettingsUI']/Docs" />
		public static void ShowSettingsUI() => Current.ShowSettingsUI();

		/// <include file="../../docs/Microsoft.Maui.Essentials/AppInfo.xml" path="//Member[@MemberName='RequestedTheme']/Docs" />
		public static AppTheme RequestedTheme => Current.RequestedTheme;

		public static AppPackagingModel PackagingModel => Current.PackagingModel;

		public static LayoutDirection RequestedLayoutDirection => Current.RequestedLayoutDirection;

		static IAppInfo? currentImplementation;

		public static IAppInfo Current =>
			currentImplementation ??= new AppInfoImplementation();

		internal static void SetCurrent(IAppInfo? implementation) =>
			currentImplementation = implementation;
	}

	public enum AppPackagingModel
	{
		Packaged,
		Unpackaged,
	}
}
