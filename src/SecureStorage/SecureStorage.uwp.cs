using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;

namespace Microsoft.Maui.Essentials
{
	public partial class SecureStorage
	{
		static async Task<string> PlatformGetAsync(string key)
		{
			var settings = GetSettings(Alias);

			var encBytes = settings.Values[key] as byte[];

			if (encBytes == null)
				return null;

			var provider = new DataProtectionProvider();

			var buffer = await provider.UnprotectAsync(encBytes.AsBuffer());

			return Encoding.UTF8.GetString(buffer.ToArray());
		}

		static async Task PlatformSetAsync(string key, string data)
		{
			var settings = GetSettings(Alias);

			var bytes = Encoding.UTF8.GetBytes(data);

			// LOCAL=user and LOCAL=machine do not require enterprise auth capability
			var provider = new DataProtectionProvider("LOCAL=user");

			var buffer = await provider.ProtectAsync(bytes.AsBuffer());

			var encBytes = buffer.ToArray();

			settings.Values[key] = encBytes;
		}

		static bool PlatformRemove(string key)
		{
			var settings = GetSettings(Alias);

			if (settings.Values.ContainsKey(key))
			{
				settings.Values.Remove(key);
				return true;
			}

			return false;
		}

		static void PlatformRemoveAll()
		{
			var settings = GetSettings(Alias);

			settings.Values.Clear();
		}

		static ApplicationDataContainer GetSettings(string name)
		{
			var localSettings = ApplicationData.Current.LocalSettings;

			if (!localSettings.Containers.ContainsKey(name))
				localSettings.CreateContainer(name, ApplicationDataCreateDisposition.Always);
			return localSettings.Containers[name];
		}
	}
}
