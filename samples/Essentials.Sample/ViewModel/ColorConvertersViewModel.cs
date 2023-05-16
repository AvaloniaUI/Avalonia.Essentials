using System;
using Avalonia.Media;

namespace Samples.ViewModel
{
	public class ColorConvertersViewModel : BaseViewModel
	{
		int alpha = 100;
		int saturation = 100;
		int hue = 360;
		int luminosity = 100;
		string hex = "#3498db";

		public ColorConvertersViewModel()
		{
		}

		public int Alpha
		{
			get => alpha;
			set => SetProperty(ref alpha, value, onChanged: SetColor);
		}

		public int Luminosity
		{
			get => luminosity;
			set => SetProperty(ref luminosity, value, onChanged: SetColor);
		}

		public int Hue
		{
			get => hue;
			set => SetProperty(ref hue, value, onChanged: SetColor);
		}

		public int Saturation
		{
			get => saturation;
			set => SetProperty(ref saturation, value, onChanged: SetColor);
		}

		public string Hex
		{
			get => hex;
			set => SetProperty(ref hex, value, onChanged: SetColor);
		}

		public Color RegularColor { get; set; }

		public Color AlphaColor { get; set; }

		public Color SaturationColor { get; set; }

		public Color HueColor { get; set; }

		public Color ComplementColor { get; set; }

		public Color LuminosityColor { get; set; }

		public string ComplementHex { get; set; }

		async void SetColor()
		{
			try
			{
				var color = Color.Parse(Hex);
				var hsl = color.ToHsl();

				RegularColor = color;
				AlphaColor = new Color((byte)(Alpha % 255), color.R, color.G, color.B);
				SaturationColor = new HslColor(hsl.A, hsl.H, Saturation / 100f, hsl.L).ToRgb();
				HueColor = new HslColor(hsl.A, Hue / 255f, hsl.S, hsl.L).ToRgb();
				LuminosityColor = new HslColor(hsl.A, hsl.H, hsl.S, Luminosity / 100f).ToRgb();
				ComplementColor = color; // TODO Avalonia
				ComplementHex = ComplementColor.ToString();

				OnPropertyChanged(nameof(RegularColor));
				OnPropertyChanged(nameof(AlphaColor));
				OnPropertyChanged(nameof(SaturationColor));
				OnPropertyChanged(nameof(HueColor));
				OnPropertyChanged(nameof(ComplementColor));
				OnPropertyChanged(nameof(LuminosityColor));
				OnPropertyChanged(nameof(ComplementHex));
			}
			catch (Exception ex)
			{
				await DisplayAlertAsync($"Unable to convert colors: {ex.Message}");
			}
		}
	}
}
