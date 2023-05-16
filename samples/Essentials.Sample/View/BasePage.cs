using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Labs.Controls;
using Samples.ViewModel;

namespace Samples.View
{
	public class BasePage : ContentPage
	{
		public BasePage()
		{
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		void OnLoaded(object sender, EventArgs e)
		{
			SetupBinding(DataContext);
		}

		void OnUnloaded(object sender, EventArgs e)
		{
			TearDownBinding(DataContext);
		}

		protected void SetupBinding(object bindingContext)
		{
			if (bindingContext is BaseViewModel vm)
			{
				vm.DoDisplayAlert += OnDisplayAlert;
				vm.DoNavigate += OnNavigate;
				vm.OnAppearing();
			}
		}

		protected void TearDownBinding(object bindingContext)
		{
			if (bindingContext is BaseViewModel vm)
			{
				vm.OnDisappearing();
				vm.DoDisplayAlert -= OnDisplayAlert;
				vm.DoNavigate -= OnNavigate;
			}
		}

		Task OnDisplayAlert(string message)
		{
			((HomePage)TopLevel.GetTopLevel(this)!.Content)!.DisplayAlert(Title, message);
			return Task.CompletedTask;
		}

		Task OnNavigate(BaseViewModel vm, bool showModal)
		{
			((HomePage)TopLevel.GetTopLevel(this)!.Content)!.Navigate(vm, showModal);
			return Task.CompletedTask;
		}
	}
}
