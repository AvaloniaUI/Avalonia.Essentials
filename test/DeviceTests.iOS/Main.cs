using System;
using UIKit;

namespace Microsoft.Maui.Essentials.DeviceTests.iOS
{
    public class Application
    {
        static void Main(string[] args)
        {
            if (args?.Length > 0 || Environment.GetEnvironmentVariable("NUNIT_AUTOEXIT")?.Length > 0) // usually means this is from xharness
                UIApplication.Main(args, null, nameof(TestApplicationDelegate));
            else
                UIApplication.Main(args, null, nameof(AppDelegate));
        }
    }
}
