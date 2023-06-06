using Samples.ViewModel;

namespace Samples.View
{
	public partial class CameraPage : BasePage
	{
		public CameraPage()
		{
			InitializeComponent();
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			if(DataContext is CameraViewModel viewModel)
			{
				viewModel.CameraPreviewer = Previewer;
			}
		}
	}
}
