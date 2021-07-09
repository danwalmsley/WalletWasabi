using Avalonia;

namespace AvaloniaWinUI.ColorPicker
{
	public partial class ColorPickerSlider
	{
		public static readonly StyledProperty<ColorPickerHsvChannel> ColorChannelProperty =
			AvaloniaProperty.Register<ColorPickerSlider, ColorPickerHsvChannel>(nameof(ColorChannel),
				ColorPickerHsvChannel.Value);

		public ColorPickerHsvChannel ColorChannel
		{
			get => GetValue(ColorChannelProperty);
			set => SetValue(ColorChannelProperty, value);
		}
	}
}