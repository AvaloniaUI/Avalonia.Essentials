using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Templates;
using Avalonia.Labs.Controls;
using Samples.Model;
using Samples.ViewModel;

namespace Samples.View;

public class PageLocator : IDataTemplate
{
	public Control Build(object param)
	{
		var item = (SampleItem)param;
		
		var pageType = item.PageType;
		var page = (BasePage)Activator.CreateInstance(pageType)!;
		page.DataContext = param;
		return page;
	}

	public bool Match(object data) => data is SampleItem;
}

public partial class HomePage : PageNavigationHost
{
	private static WindowNotificationManager _manager;
	public HomePage()
	{
		InitializeComponent();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		
		if (_manager is null)
		{
			var topLevel = TopLevel.GetTopLevel(this);
			_manager = new WindowNotificationManager(topLevel);
		}
	}

	public void DisplayAlert(string title, string message)
	{
		_manager.Show(new Notification(title, message));
	}
	
	public void Navigate(BaseViewModel vm, bool showModal)
	{
		var name = vm.GetType().Name;
		name = name.Replace("ViewModel", "Page", StringComparison.Ordinal);
		var ns = GetType().Namespace;
		var pageType = Type.GetType($"{ns}.{name}");
		var page = (BasePage)Activator.CreateInstance(pageType)!;
		page.DataContext = vm;

		NavigationList.SelectedItem = null;
		ContentPage.Push(page);
	}
	
	public void Navigate(Type pageType)
	{
		var item = ((HomeViewModel)DataContext).AllItems.FirstOrDefault(p => p.PageType == pageType);
		NavigationList.SelectedItem = item;
	}

	private void NavigationList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var sampleItem = (SampleItem)NavigationList.SelectedItem;
		if (sampleItem is not null)
		{
			var page = (BasePage)Activator.CreateInstance(sampleItem.PageType)!;
			ContentPage.Push(page);
		}
	}
}