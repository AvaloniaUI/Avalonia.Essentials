using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace Microsoft.Maui.ApplicationModel
{
	public static partial class Permissions
	{
		public static bool IsDeclaredInManifest(string permission)
		{
			var context = Application.Context;
			var packageInfo = context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.Permissions);
			var requestedPermissions = packageInfo?.RequestedPermissions;

			return requestedPermissions?.Any(r => r.Equals(permission, StringComparison.OrdinalIgnoreCase)) ?? false;
		}

		internal static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
			=> BasePlatformPermission.OnRequestPermissionsResult(requestCode, permissions, grantResults);

		public abstract partial class BasePlatformPermission : BasePermission
		{
			static readonly Dictionary<string, (int requestCode, TaskCompletionSource<PermissionStatus> tcs)> requests =
				new Dictionary<string, (int, TaskCompletionSource<PermissionStatus>)>();

			static readonly object locker = new object();
			static int requestCode;

			public virtual (string androidPermission, bool isRuntime)[] RequiredPermissions { get; }

			public override Task<PermissionStatus> CheckStatusAsync()
			{
				if (RequiredPermissions == null || RequiredPermissions.Length <= 0)
					return Task.FromResult(PermissionStatus.Granted);

				foreach (var (androidPermission, isRuntime) in RequiredPermissions)
				{
					var ap = androidPermission;
					if (!IsDeclaredInManifest(ap))
						throw new PermissionException($"You need to declare using the permission: `{androidPermission}` in your AndroidManifest.xml");

					var status = DoCheck(ap);
					if (status != PermissionStatus.Granted)
						return Task.FromResult(PermissionStatus.Denied);
				}

				return Task.FromResult(PermissionStatus.Granted);
			}

			public override async Task<PermissionStatus> RequestAsync()
			{
				// Check status before requesting first
				if (await CheckStatusAsync() == PermissionStatus.Granted)
					return PermissionStatus.Granted;

				TaskCompletionSource<PermissionStatus> tcs;
				var doRequest = true;

				var runtimePermissions = RequiredPermissions.Where(p => p.isRuntime)
					?.Select(p => p.androidPermission)?.ToArray();

				// We may have no runtime permissions required, in this case
				// knowing they all exist in the manifest from the Check call above is sufficient
				if (runtimePermissions == null || !runtimePermissions.Any())
					return PermissionStatus.Granted;

				var permissionId = string.Join(';', runtimePermissions);

				lock (locker)
				{
					if (requests.ContainsKey(permissionId))
					{
						tcs = requests[permissionId].tcs;
						doRequest = false;
					}
					else
					{
						tcs = new TaskCompletionSource<PermissionStatus>();

						requestCode = PlatformUtils.NextRequestCode();

						requests.Add(permissionId, (requestCode, tcs));
					}
				}

				if (!doRequest)
					return await tcs.Task;

				if (!MainThread.IsMainThread)
					throw new PermissionException("Permission request must be invoked on main thread.");

				ActivityCompat.RequestPermissions(ActivityStateManager.Default.GetCurrentActivity(true), runtimePermissions.ToArray(), requestCode);

				var result = await tcs.Task;

				if (requests.ContainsKey(permissionId))
					requests.Remove(permissionId);

				return result;
			}

			public override void EnsureDeclared()
			{
				if (RequiredPermissions == null || RequiredPermissions.Length <= 0)
					return;

				foreach (var (androidPermission, isRuntime) in RequiredPermissions)
				{
					var ap = androidPermission;
					if (!IsDeclaredInManifest(ap))
						throw new PermissionException($"You need to declare using the permission: `{androidPermission}` in your AndroidManifest.xml");
				}
			}

			public override bool ShouldShowRationale()
			{
				if (RequiredPermissions == null || RequiredPermissions.Length <= 0)
					return false;

				var activity = ActivityStateManager.Default.GetCurrentActivity(true);
				foreach (var (androidPermission, isRuntime) in RequiredPermissions)
				{
					if (isRuntime && ActivityCompat.ShouldShowRequestPermissionRationale(activity, androidPermission))
						return true;
				}

				return false;
			}

			protected virtual PermissionStatus DoCheck(string androidPermission)
			{
				var context = Platform.AppContext;
				var targetsMOrHigher = context.ApplicationInfo.TargetSdkVersion >= BuildVersionCodes.M;

				if (!IsDeclaredInManifest(androidPermission))
					throw new PermissionException($"You need to declare using the permission: `{androidPermission}` in your AndroidManifest.xml");

				var status = PermissionStatus.Granted;

				if (targetsMOrHigher)
				{
					status = ContextCompat.CheckSelfPermission(context, androidPermission) switch
					{
						Permission.Granted => PermissionStatus.Granted,
						Permission.Denied => PermissionStatus.Denied,
						_ => PermissionStatus.Unknown
					};
				}
				else
				{
					status = PermissionChecker.CheckSelfPermission(context, androidPermission) switch
					{
						PermissionChecker.PermissionGranted => PermissionStatus.Granted,
						PermissionChecker.PermissionDenied => PermissionStatus.Denied,
						PermissionChecker.PermissionDeniedAppOp => PermissionStatus.Denied,
						_ => PermissionStatus.Unknown
					};
				}
				return status;
			}

			internal static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
			{
				lock (locker)
				{
					// Check our pending requests for one with a matching request code
					foreach (var kvp in requests)
					{
						if (kvp.Value.requestCode == requestCode)
						{
							var tcs = kvp.Value.tcs;

							// Look for any denied requests, and deny the whole request if so
							// Remember, each PermissionType is tied to 1 or more android permissions
							// so if any android permissions denied the whole PermissionType is considered denied
							if (grantResults.Any(g => g == Permission.Denied))
								tcs.TrySetResult(PermissionStatus.Denied);
							else
								tcs.TrySetResult(PermissionStatus.Granted);
							break;
						}
					}
				}
			}
		}

		public partial class Battery : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.BatteryStats, false) };

			public override Task<PermissionStatus> CheckStatusAsync() =>
				Task.FromResult(IsDeclaredInManifest(Manifest.Permission.BatteryStats) ? PermissionStatus.Granted : PermissionStatus.Denied);
		}

		public partial class CalendarRead : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.ReadCalendar, true) };
		}

		public partial class CalendarWrite : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.WriteCalendar, true) };
		}

		public partial class Camera : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.Camera, true) };
		}

		public partial class ContactsRead : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.ReadContacts, true) };
		}

		public partial class ContactsWrite : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.WriteContacts, true) };
		}

		public partial class Flashlight : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[]
				{
					(Manifest.Permission.Camera, true),
					(Manifest.Permission.Flashlight, false)
				};
		}

		public partial class LaunchApp : BasePlatformPermission
		{
		}

		public partial class LocationWhenInUse : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[]
				{
					(Manifest.Permission.AccessCoarseLocation, true),
					(Manifest.Permission.AccessFineLocation, true)
				};

			public override Task<PermissionStatus> CheckStatusAsync()
			{
				if (DoCheck(Manifest.Permission.AccessFineLocation) == PermissionStatus.Granted)
					return Task.FromResult(PermissionStatus.Granted);

				if (DoCheck(Manifest.Permission.AccessCoarseLocation) == PermissionStatus.Granted)
					return Task.FromResult(PermissionStatus.Restricted);

				return Task.FromResult(PermissionStatus.Denied);
			}
		}

		public partial class LocationAlways : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions
			{
				get
				{
					var permissions = new List<(string, bool)>();
#if __ANDROID_29__
					// Check if running and targeting Q
					if (OperatingSystem.IsAndroidVersionAtLeast(29) && Application.Context.ApplicationInfo.TargetSdkVersion >= BuildVersionCodes.Q)
						permissions.Add((Manifest.Permission.AccessBackgroundLocation, true));
#endif

					permissions.Add((Manifest.Permission.AccessCoarseLocation, true));
					permissions.Add((Manifest.Permission.AccessFineLocation, true));

					return permissions.ToArray();
				}
			}
		}

		public partial class Maps : BasePlatformPermission
		{
		}

		public partial class Media : BasePlatformPermission
		{
		}

		public partial class Microphone : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.RecordAudio, true) };
		}

		public partial class NetworkState : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions
			{
				get
				{
					var permissions = new List<(string, bool)>
					{
						(Manifest.Permission.AccessNetworkState, false)
					};

					if (IsDeclaredInManifest(Manifest.Permission.ChangeNetworkState))
						permissions.Add((Manifest.Permission.ChangeNetworkState, true));

					return permissions.ToArray();
				}
			}
		}

		public partial class Phone : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions
			{
				get
				{
					var permissions = new List<(string, bool)>
					{
						(Manifest.Permission.ReadPhoneState, true)
					};

					if (IsDeclaredInManifest(Manifest.Permission.CallPhone))
						permissions.Add((Manifest.Permission.CallPhone, true));
					if (IsDeclaredInManifest(Manifest.Permission.ReadCallLog))
						permissions.Add((Manifest.Permission.ReadCallLog, true));
					if (IsDeclaredInManifest(Manifest.Permission.WriteCallLog))
						permissions.Add((Manifest.Permission.WriteCallLog, true));
					if (IsDeclaredInManifest(Manifest.Permission.AddVoicemail))
						permissions.Add((Manifest.Permission.AddVoicemail, true));
					if (IsDeclaredInManifest(Manifest.Permission.UseSip))
						permissions.Add((Manifest.Permission.UseSip, true));
					if (OperatingSystem.IsAndroidVersionAtLeast(26))
					{
						if (IsDeclaredInManifest(Manifest.Permission.AnswerPhoneCalls))
							permissions.Add((Manifest.Permission.AnswerPhoneCalls, true));
					}

#pragma warning disable CS0618 // Type or member is obsolete
					if (IsDeclaredInManifest(Manifest.Permission.ProcessOutgoingCalls))
					{
						if (OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.Q))
							System.Diagnostics.Debug.WriteLine($"{Manifest.Permission.ProcessOutgoingCalls} is deprecated in Android 10");
						permissions.Add((Manifest.Permission.ProcessOutgoingCalls, true));
					}
#pragma warning restore CS0618 // Type or member is obsolete

					return permissions.ToArray();
				}
			}
		}

		public partial class Photos : BasePlatformPermission
		{
		}

		public partial class PhotosAddOnly : BasePlatformPermission
		{
		}

		public partial class Reminders : BasePlatformPermission
		{
		}

		public partial class Sensors : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.BodySensors, true) };
		}

		public partial class Sms : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions
			{
				get
				{
					var permissions = new List<(string, bool)>
					{
						(Manifest.Permission.ReceiveSms, true)
					};

					if (IsDeclaredInManifest(Manifest.Permission.SendSms))
						permissions.Add((Manifest.Permission.SendSms, true));
					if (IsDeclaredInManifest(Manifest.Permission.ReadSms))
						permissions.Add((Manifest.Permission.ReadSms, true));
					if (IsDeclaredInManifest(Manifest.Permission.ReceiveWapPush))
						permissions.Add((Manifest.Permission.ReceiveWapPush, true));
					if (IsDeclaredInManifest(Manifest.Permission.ReceiveMms))
						permissions.Add((Manifest.Permission.ReceiveMms, true));

					return permissions.ToArray();
				}
			}
		}

		public partial class Speech : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.RecordAudio, true) };
		}

		public partial class StorageRead : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.ReadExternalStorage, true) };
		}

		public partial class StorageWrite : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.WriteExternalStorage, true) };
		}

		public partial class Vibrate : BasePlatformPermission
		{
			public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
				new (string, bool)[] { (Manifest.Permission.Vibrate, false) };
		}
	}
}
