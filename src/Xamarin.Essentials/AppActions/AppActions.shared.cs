﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Essentials
{
	public static partial class AppActions
	{
		internal static bool IsSupported
			=> PlatformIsSupported;

		public static Task<IEnumerable<AppAction>> GetAsync()
			=> PlatformGetAsync();

		public static Task SetAsync(params AppAction[] actions)
			=> PlatformSetAsync(actions);

		public static Task SetAsync(IEnumerable<AppAction> actions)
			=> PlatformSetAsync(actions);

		public static event EventHandler<AppActionEventArgs> OnAppAction;

		internal static void InvokeOnAppAction(object sender, AppAction appAction)
			=> OnAppAction?.Invoke(sender, new AppActionEventArgs(appAction));
	}

	public class AppActionEventArgs : EventArgs
	{
		public AppActionEventArgs(AppAction appAction)
			: base() => AppAction = appAction;

		public AppAction AppAction { get; }
	}

	public class AppAction
	{
		public AppAction(string id, string title, string subtitle = null, string icon = null)
		{
			Id = id ?? throw new ArgumentNullException(nameof(id));
			Title = title ?? throw new ArgumentNullException(nameof(title));

			Subtitle = subtitle;
			Icon = icon;
		}

		public string Title { get; set; }

		public string Subtitle { get; set; }

		public string Id { get; set; }

		internal string Icon { get; set; }
	}
}
