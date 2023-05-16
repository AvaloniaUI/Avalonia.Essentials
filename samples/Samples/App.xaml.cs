using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Maui.ApplicationModel;
using Samples.View;

namespace Samples
{
	public partial class App : Application
	{
		private static HomePage _host;

		public App()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			_host = new HomePage();
			
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				desktop.MainWindow = new MainWindow
				{
					Content = _host
				};
			}
			else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
			{
				singleView.MainView = _host;
			}

			base.OnFrameworkInitializationCompleted();
		}

		public static void HandleAppActions(AppAction appAction)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				var page = appAction.Id switch
				{
					"battery_info" => typeof(BatteryPage),
					"app_info" => typeof(AppInfoPage),
					_ => null
				};

				if (page != null)
				{
					_host?.Navigate(page);
				}
			});
		}
	}
}
