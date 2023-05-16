using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Avalonia.Android;
using Microsoft.Maui.ApplicationModel;

namespace Essentials.Sample.Android;

[Activity(Label = "Essentials.Sample.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		Platform.Init(this, savedInstanceState);
		Platform.ActivityStateChanged += Platform_ActivityStateChanged;
	}
	
	protected override void OnResume()
	{
		base.OnResume();

		Platform.OnResume(this);
	}

	protected override void OnNewIntent(Intent intent)
	{
		base.OnNewIntent(intent);

		Platform.OnNewIntent(intent);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		Platform.ActivityStateChanged -= Platform_ActivityStateChanged;
	}

	void Platform_ActivityStateChanged(object sender, ActivityStateChangedEventArgs e) =>
		Toast.MakeText(this, e.State.ToString(), ToastLength.Short).Show();

	public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
	{
		Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

		base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
	}
}

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
	new[] { Intent.ActionView },
	Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
	DataScheme = "xamarinessentials")]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}
 