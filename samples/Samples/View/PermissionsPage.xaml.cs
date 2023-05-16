using System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.ApplicationModel;
using Samples.ViewModel;

namespace Samples.View
{
	public partial class PermissionsPage : BasePage
	{
		public PermissionsPage()
		{
			InitializeComponent();
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			WeakReferenceMessenger.Default.Register<Exception, string>(
				this,
				nameof(PermissionException),
				async (p, ex) => await ((PermissionsViewModel)DataContext!).DisplayAlertAsync(ex.Message));
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			WeakReferenceMessenger.Default.Unregister<Exception, string>(this, nameof(PermissionException));
		}
	}
}
