using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvaloniaWinUI.ColorPicker
{
	public partial class ColorPickerSlider : Slider, IStyleable
	{
		private Thumb? _thumb;

		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
		{
			base.OnPropertyChanged(change);

			if (change.Property == ValueProperty)
			{
				OnValueChanged();
			}
		}

		protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
		{
			base.OnApplyTemplate(e);

			_thumb = e.NameScope.Find<Thumb>("thumb");

			if (ToolTip.GetTip(_thumb) is ToolTip toolTip)
			{
				toolTip.Content = GetToolTipString();
			}
		}

		protected override void OnKeyDown(KeyEventArgs args)
		{
			if (Orientation == Orientation.Horizontal &&
			    args.Key != Key.Left &&
			    args.Key != Key.Right ||
			    Orientation == Orientation.Vertical &&
			    args.Key != Key.Up &&
			    args.Key != Key.Down)
			{
				base.OnKeyDown(args);
				return;
			}

			var parentColorPicker = GetParentColorPicker();

			if (parentColorPicker == null)
			{
				return;
			}

			var isControlDown = args.KeyModifiers.HasFlagCustom(KeyModifiers.Control);

			double minBound;
			double maxBound;

			var currentHsv = parentColorPicker.CurrentHsv;
			double currentAlpha = 0;

			switch (ColorChannel)
			{
				case ColorPickerHsvChannel.Hue:
					minBound = parentColorPicker.MinHue;
					maxBound = parentColorPicker.MaxHue;
					currentHsv.H = Value;
					break;

				case ColorPickerHsvChannel.Saturation:
					minBound = parentColorPicker.MinSaturation;
					maxBound = parentColorPicker.MaxSaturation;
					currentHsv.S = Value / 100;
					break;

				case ColorPickerHsvChannel.Value:
					minBound = parentColorPicker.MinValue;
					maxBound = parentColorPicker.MaxValue;
					currentHsv.V = Value / 100;
					break;

				case ColorPickerHsvChannel.Alpha:
					minBound = 0;
					maxBound = 100;
					currentAlpha = Value / 100;
					break;

				default:
					throw new NotSupportedException("Invalid ColorPickerHsvChannel.");
			}

			// TODO: FlowDirection is missed in Avalonia
			var shouldInvertHorizontalDirection = false;

			var direction =
				args.Key == Key.Left && !shouldInvertHorizontalDirection ||
				args.Key == Key.Right && shouldInvertHorizontalDirection ||
				args.Key == Key.Up
					? IncrementDirection.Lower
					: IncrementDirection.Higher;

			var amount = isControlDown ? IncrementAmount.Large : IncrementAmount.Small;

			if (ColorChannel != ColorPickerHsvChannel.Alpha)
			{
				currentHsv = ColorHelpers.IncrementColorChannel(currentHsv, ColorChannel, direction, amount,
					false /* shouldWrap */, minBound, maxBound);
			}
			else
			{
				currentAlpha = ColorHelpers.IncrementAlphaChannel(currentAlpha, direction, amount,
					false /* shouldWrap */, minBound, maxBound);
			}

			Value = ColorChannel switch
			{
				ColorPickerHsvChannel.Hue => currentHsv.H,
				ColorPickerHsvChannel.Saturation => currentHsv.S * 100,
				ColorPickerHsvChannel.Value => currentHsv.V * 100,
				ColorPickerHsvChannel.Alpha => currentAlpha * 100,
				_ => throw new NotSupportedException("Invalid ColorPickerHsvChannel.")
			};

			args.Handled = true;
		}

		protected override void OnGotFocus(GotFocusEventArgs e)
		{
			if (_thumb != null
			    && ToolTip.GetTip(_thumb) is ToolTip toolTip)
			{
				toolTip.Content = GetToolTipString();
				ToolTip.SetIsOpen(_thumb, true);
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			if (_thumb != null)
			{
				ToolTip.SetIsOpen(_thumb, false);
			}
		}

		private void OnValueChanged()
		{
			if (_thumb != null
			    && ToolTip.GetTip(_thumb) is ToolTip toolTip)
			{
				toolTip.Content = GetToolTipString();
			}
		}

		private ColorPicker GetParentColorPicker()
		{
			return this.FindAncestorOfType<ColorPicker>();
		}

		private string GetToolTipString()
		{
			var sliderValue = (uint) Math.Round(Value);

			if (ColorChannel == ColorPickerHsvChannel.Alpha)
			{
				return string.Format(
					CultureInfo.CurrentCulture,
					LocalizedStrings.ToolTipStringAlphaSlider,
					sliderValue);
			}

			var parentColorPicker = GetParentColorPicker();
			if (parentColorPicker != null)
			{
				var currentHsv = parentColorPicker.CurrentHsv;
				string localizedString;

				switch (ColorChannel)
				{
					case ColorPickerHsvChannel.Hue:
						currentHsv.H = Value;
						localizedString = LocalizedStrings.ToolTipStringHueSliderWithColorName;
						break;

					case ColorPickerHsvChannel.Saturation:
						localizedString = LocalizedStrings.ToolTipStringSaturationSliderWithColorName;
						currentHsv.S = Value / 100;
						break;

					case ColorPickerHsvChannel.Value:
						localizedString = LocalizedStrings.ToolTipStringValueSliderWithColorName;
						currentHsv.V = Value / 100;
						break;
					default:
						throw new NotSupportedException("Invalid ColorPickerHsvChannel.");
				}

				return string.Format(
					CultureInfo.CurrentCulture,
					localizedString,
					sliderValue,
					ColorHelpers.ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(currentHsv))));
			}
			else
			{
				var localizedString = ColorChannel switch
				{
					ColorPickerHsvChannel.Hue => LocalizedStrings.ToolTipStringHueSliderWithoutColorName,
					ColorPickerHsvChannel.Saturation => LocalizedStrings.ToolTipStringSaturationSliderWithoutColorName,
					ColorPickerHsvChannel.Value => LocalizedStrings.ToolTipStringValueSliderWithoutColorName,
					_ => throw new NotSupportedException("Invalid ColorPickerHsvChannel.")
				};

				return string.Format(
					CultureInfo.CurrentCulture,
					localizedString,
					sliderValue);
			}
		}
	}
}