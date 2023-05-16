namespace Samples.View
{
	public partial class AllSensorsPage : BasePage
	{
		public AllSensorsPage()
		{
			InitializeComponent();
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			SetupBinding(GridAccelerometer.DataContext);
			SetupBinding(GridCompass.DataContext);
			SetupBinding(GridGyro.DataContext);
			SetupBinding(GridMagnetometer.DataContext);
			SetupBinding(GridOrientation.DataContext);
			SetupBinding(GridBarometer.DataContext);
		}

		protected override void OnUnloaded()
		{
			TearDownBinding(GridAccelerometer.DataContext);
			TearDownBinding(GridCompass.DataContext);
			TearDownBinding(GridGyro.DataContext);
			TearDownBinding(GridMagnetometer.DataContext);
			TearDownBinding(GridOrientation.DataContext);
			TearDownBinding(GridBarometer.DataContext);

			base.OnUnloaded();
		}
	}
}
