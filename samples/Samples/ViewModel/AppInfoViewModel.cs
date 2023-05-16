using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;

namespace Samples.ViewModel
{
	public class AppInfoViewModel : BaseViewModel
	{
		public string AppPackageName => AppInfo.PackageName;

		public string AppName => AppInfo.Name;

		public string AppVersion => AppInfo.VersionString;

		public string AppBuild => AppInfo.BuildString;

		public string AppTheme => AppInfo.RequestedTheme.ToString();

		public RelayCommand ShowSettingsUICommand { get; }

		public AppInfoViewModel()
		{
			ShowSettingsUICommand = new RelayCommand(() => AppInfo.ShowSettingsUI());
		}
	}
}
