using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Samples.View;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		Task.Run(async () =>
		{
			/*await Task.Delay(3000);

			object? content = null;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				content = Content;
				Content = null;
			});
			await Task.Delay(1000);

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				Content = content;
			});*/
		});
	}
}