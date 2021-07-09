using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media;

namespace AvaloniaWinUI.ColorPicker
{
	public partial class ColorSpectrum
	{
		//
		// Summary:
		//     Identifies the Color dependency property.
		//
		// Returns:
		//     The identifier for the Color dependency property.
		public static readonly StyledProperty<Color> ColorProperty =
			AvaloniaProperty.Register<ColorPicker, Color>(nameof(Color), Colors.White);

		//
		// Summary:
		//     Identifies the Color dependency property.
		//
		// Returns:
		//     The identifier for the Color dependency property.
		public static readonly StyledProperty<Vector4> HsvColorProperty =
			AvaloniaProperty.Register<ColorPicker, Vector4>(nameof(HsvColor), new Vector4(0, 0, 1, 1));

		//
		// Summary:
		//     Identifies the ColorSpectrumComponents dependency property.
		//
		// Returns:
		//     The identifier for the ColorSpectrumComponents dependency property.
		public static readonly StyledProperty<ColorSpectrumComponents> ComponentsProperty =
			AvaloniaProperty.Register<ColorPicker, ColorSpectrumComponents>(nameof(Components),
				ColorSpectrumComponents.HueSaturation);

		//
		// Summary:
		//     Identifies the ColorSpectrumShape dependency property.
		//
		// Returns:
		//     The identifier for the ColorSpectrumShape dependency property.
		public static readonly StyledProperty<ColorSpectrumShape> ShapeProperty =
			AvaloniaProperty.Register<ColorPicker, ColorSpectrumShape>(nameof(Shape));

		//
		// Summary:
		//     Identifies the MaxHue dependency property.
		//
		// Returns:
		//     The identifier for the MaxHue dependency property.
		public static readonly StyledProperty<int> MaxHueProperty =
			AvaloniaProperty.Register<ColorPicker, int>(nameof(MaxHue), 359,
				validate: value => value >= 0 && value <= 359);

		//
		// Summary:
		//     Identifies the MaxSaturation dependency property.
		//
		// Returns:
		//     The identifier for the MaxSaturation dependency property.
		public static readonly StyledProperty<int> MaxSaturationProperty =
			AvaloniaProperty.Register<ColorPicker, int>(nameof(MaxSaturation), 100,
				validate: value => value >= 0 && value <= 100);

		//
		// Summary:
		//     Identifies the MaxValue dependency property.
		//
		// Returns:
		//     The identifier for the MaxValue dependency property.
		public static readonly StyledProperty<int> MaxValueProperty =
			AvaloniaProperty.Register<ColorPicker, int>(nameof(MaxValue), 100,
				validate: value => value >= 0 && value <= 100);

		//
		// Summary:
		//     Identifies the MinHue dependency property.
		//
		// Returns:
		//     The identifier for the MinHue dependency property.
		public static readonly StyledProperty<int> MinHueProperty =
			AvaloniaProperty.Register<ColorPicker, int>(nameof(MinHue), 0,
				validate: value => value >= 0 && value <= 359);

		//
		// Summary:
		//     Identifies the MinSaturation dependency property.
		//
		// Returns:
		//     The identifier for the MinSaturation dependency property.
		public static readonly StyledProperty<int> MinSaturationProperty =
			AvaloniaProperty.Register<ColorPicker, int>(nameof(MinSaturation), 0,
				validate: value => value >= 0 && value <= 100);

		//
		// Summary:
		//     Identifies the MinValue dependency property.
		//
		// Returns:
		//     The identifier for the MinValue dependency property.
		public static readonly StyledProperty<int> MinValueProperty =
			AvaloniaProperty.Register<ColorPicker, int>(nameof(MinValue), 0,
				validate: value => value >= 0 && value <= 100);

		//
		// Summary:
		//     Gets or sets the minimum Value value in the range 0-100.
		//
		// Returns:
		//     The minimum Value value in the range 0-100. The default is 100.
		public int MinValue
		{
			get => GetValue(MinValueProperty);
			set => SetValue(MinValueProperty, value);
		}

		//
		// Summary:
		//     Gets or sets the minimum Saturation value in the range 0-100.
		//
		// Returns:
		//     The minimum Saturation value in the range 0-100. The default is 100.
		public int MinSaturation
		{
			get => GetValue(MinSaturationProperty);
			set => SetValue(MinSaturationProperty, value);
		}

		//
		// Summary:
		//     Gets or sets the minimum Hue value in the range 0-359.
		//
		// Returns:
		//     The minimum Hue value in the range 0-359. The default is 0.
		public int MinHue
		{
			get => GetValue(MinHueProperty);
			set => SetValue(MinHueProperty, value);
		}

		//
		// Summary:
		//     Gets or sets the maximum Value value in the range 0-100.
		//
		// Returns:
		//     The maximum Value value in the range 0-100. The default is 100.
		public int MaxValue
		{
			get => GetValue(MaxValueProperty);
			set => SetValue(MaxValueProperty, value);
		}

		//
		// Summary:
		//     Gets or sets the maximum Saturation value in the range 0-100.
		//
		// Returns:
		//     The maximum Saturation value in the range 0-100. The default is 100.
		public int MaxSaturation
		{
			get => GetValue(MaxSaturationProperty);
			set => SetValue(MaxSaturationProperty, value);
		}

		//
		// Summary:
		//     Gets or sets the maximum Hue value in the range 0-359.
		//
		// Returns:
		//     The maximum Hue value in the range 0-359. The default is 359.
		public int MaxHue
		{
			get => GetValue(MaxHueProperty);
			set => SetValue(MaxHueProperty, value);
		}

		//
		// Summary:
		//     Gets or sets a value that indicates whether the ColorSpectrum is shown as a square
		//     or a circle.
		//
		// Returns:
		//     A value of the enumeration. The default is **Box**, which shows the spectrum
		//     as a square.
		public ColorSpectrumShape Shape
		{
			get => GetValue(ShapeProperty);
			set => SetValue(ShapeProperty, value);
		}

		//
		// Summary:
		//     Gets or sets a value that indicates how the Hue-Saturation-Value (HSV) color
		//     components are mapped onto the ColorSpectrum.
		//
		// Returns:
		//     A value of the enumeration. The default is **HueSaturation**.
		public ColorSpectrumComponents Components
		{
			get => GetValue(ComponentsProperty);
			set => SetValue(ComponentsProperty, value);
		}

		//
		// Summary:
		//     Gets or sets the current color value.
		//
		// Returns:
		//     The current color value.
		public Color Color
		{
			get => GetValue(ColorProperty);
			set => SetValue(ColorProperty, value);
		}

		//
		// Summary:
		//     Gets or sets the current color value as a Vector4.
		//
		// Returns:
		//     The current HSV color value.
		public Vector4 HsvColor
		{
			get => GetValue(HsvColorProperty);
			set => SetValue(HsvColorProperty, value);
		}

		//
		// Summary:
		//     Occurs when the Color property has changed.
		public event EventHandler<ColorChangedEventArgs>? ColorChanged;
	}
}