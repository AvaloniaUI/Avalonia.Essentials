﻿using System;
using System.Diagnostics;
using Tizen.System;

namespace Xamarin.Essentials
{
	public static partial class HapticFeedback
	{
		internal static bool IsSupported => true;

		static void PlatformPerform(HapticFeedbackType type)
		{
			Permissions.EnsureDeclared<Permissions.Vibrate>();
			try
			{
				var feedback = new Feedback();
				var pattern = ConvertType(type);
				if (feedback.IsSupportedPattern(FeedbackType.Vibration, pattern))
					feedback.Play(FeedbackType.Vibration, pattern);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"HapticFeedback Exception: {ex.Message}");
			}
		}

		static string ConvertType(HapticFeedbackType type) =>
			type switch
			{
				HapticFeedbackType.LongPress => "Hold",
				_ => "Tap"
			};
	}
}
