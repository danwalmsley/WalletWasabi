using System;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvaloniaWinUI.ColorPicker
{
	internal enum ColorUpdateReason
	{
		InitializingColor,
		ColorPropertyChanged,
		ColorSpectrumColorChanged,
		ThirdDimensionSliderChanged,
		AlphaSliderChanged,
		RgbTextBoxChanged,
		HsvTextBoxChanged,
		AlphaTextBoxChanged,
		HexTextBoxChanged
	}

	public partial class ColorPicker : TemplatedControl
	{
		private TextBlock? m_alphaLabel;

		private ColorPickerSlider? m_alphaSlider;
		private TextBox? m_alphaTextBox;
		private TextBlock? m_blueLabel;
		private TextBox? m_blueTextBox;

		private Rectangle? m_colorPreviewRectangle;

		// Template parts
		private ColorSpectrum? m_colorSpectrum;
		private double m_currentAlpha = 1.0;
		private string m_currentHex = "#FFFFFFFF";
		private Hsv m_currentHsv = new(0.0, 1.0, 1.0);
		private Rgb m_currentRgb = new(1.0, 1.0, 1.0);
		private TextBlock? m_greenLabel;
		private TextBox? m_greenTextBox;
		private TextBox? m_hexTextBox;
		private TextBlock? m_hueLabel;
		private TextBox? m_hueTextBox;
		private bool m_isFocusedTextBoxValid;

		private ToggleButton? m_moreButton;
		private TextBlock? m_moreButtonLabel;
		private Rectangle? m_previousColorRectangle;

		private string m_previousString = string.Empty;

		private TextBlock? m_redLabel;

		private TextBox? m_redTextBox;
		private TextBlock? m_saturationLabel;
		private TextBox? m_saturationTextBox;

		private bool m_textEntryGridOpened;

		private ColorPickerSlider? m_thirdDimensionSlider;
		private LinearGradientBrush? m_thirdDimensionSliderGradientBrush;
		private bool m_updatingColor;
		private bool m_updatingControls;
		private TextBlock? m_valueLabel;
		private TextBox? m_valueTextBox;

		public int OnColorSpectrumSizeChanged { get; private set; }

		protected override void OnApplyTemplate(TemplateAppliedEventArgs args)
		{
			m_colorSpectrum = args.NameScope.Get<ColorSpectrum>("PART_ColorSpectrum");

			m_colorPreviewRectangle = args.NameScope.Get<Rectangle>("PART_ColorPreviewRectangle");
			m_previousColorRectangle = args.NameScope.Get<Rectangle>("PART_PreviousColorRectangle");

			m_thirdDimensionSlider = args.NameScope.Get<ColorPickerSlider>("PART_ThirdDimensionSlider");
			m_thirdDimensionSliderGradientBrush =
				args.NameScope.Get<Rectangle>("PART_ThirdDimensionSliderRectangle")?.Fill as LinearGradientBrush;

			m_alphaSlider = args.NameScope.Get<ColorPickerSlider>("PART_AlphaSlider");

			m_moreButton = args.NameScope.Get<ToggleButton>("PART_MoreButton");

			m_redTextBox = args.NameScope.Get<TextBox>("PART_RedTextBox");
			m_greenTextBox = args.NameScope.Get<TextBox>("PART_GreenTextBox");
			m_blueTextBox = args.NameScope.Get<TextBox>("PART_BlueTextBox");
			m_hueTextBox = args.NameScope.Get<TextBox>("PART_HueTextBox");
			m_saturationTextBox = args.NameScope.Get<TextBox>("PART_SaturationTextBox");
			m_valueTextBox = args.NameScope.Get<TextBox>("PART_ValueTextBox");
			m_alphaTextBox = args.NameScope.Get<TextBox>("PART_AlphaTextBox");
			m_hexTextBox = args.NameScope.Get<TextBox>("PART_HexTextBox");

			m_redLabel = args.NameScope.Get<TextBlock>("PART_RedLabel");
			m_greenLabel = args.NameScope.Get<TextBlock>("PART_GreenLabel");
			m_blueLabel = args.NameScope.Get<TextBlock>("PART_BlueLabel");
			m_hueLabel = args.NameScope.Get<TextBlock>("PART_HueLabel");
			m_saturationLabel = args.NameScope.Get<TextBlock>("PART_SaturationLabel");
			m_valueLabel = args.NameScope.Get<TextBlock>("PART_ValueLabel");
			m_alphaLabel = args.NameScope.Get<TextBlock>("PART_AlphaLabel");

			if (m_colorSpectrum != null)
			{
				m_colorSpectrum.ColorChanged += OnColorSpectrumColorChanged;
				m_colorSpectrum.PropertyChanged += OnColorSpectrumPropertyChanged;
			}

			if (m_thirdDimensionSlider != null)
			{
				m_thirdDimensionSlider.PropertyChanged += OnThirdDimensionSliderPropertyChanged;
				SetThirdDimensionSliderChannel();
			}

			if (m_alphaSlider != null)
			{
				m_alphaSlider.PropertyChanged += OnAlphaSliderPropertyChanged;
				m_alphaSlider.ColorChannel = ColorPickerHsvChannel.Alpha;
			}

			if (m_moreButton != null)
			{
				m_moreButton.Checked += OnMoreButtonChecked;
				m_moreButton.Unchecked += OnMoreButtonUnchecked;

				if (args.NameScope.Get<TextBlock>("PART_MoreButtonLabel") is TextBlock moreButtonLabel)
				{
					m_moreButtonLabel = moreButtonLabel;
					moreButtonLabel.Text = LocalizedStrings.TextMoreButtonLabelCollapsed;
				}
			}

			if (m_redTextBox != null)
			{
				m_redTextBox.TextInput += OnRgbTextChanging;
				m_redTextBox.GotFocus += OnTextBoxGotFocus;
				m_redTextBox.LostFocus += OnTextBoxLostFocus;
			}

			if (m_greenTextBox != null)
			{
				m_greenTextBox.TextInput += OnRgbTextChanging;
				m_greenTextBox.GotFocus += OnTextBoxGotFocus;
				m_greenTextBox.LostFocus += OnTextBoxLostFocus;
			}

			if (m_blueTextBox != null)
			{
				m_blueTextBox.TextInput += OnRgbTextChanging;
				m_blueTextBox.GotFocus += OnTextBoxGotFocus;
				m_blueTextBox.LostFocus += OnTextBoxLostFocus;
			}

			if (m_hueTextBox != null)
			{
				m_hueTextBox.TextInput += OnHueTextChanging;
				m_hueTextBox.GotFocus += OnTextBoxGotFocus;
				m_hueTextBox.LostFocus += OnTextBoxLostFocus;
			}

			if (m_saturationTextBox != null)
			{
				m_saturationTextBox.TextInput += OnSaturationTextChanging;
				m_saturationTextBox.GotFocus += OnTextBoxGotFocus;
				m_saturationTextBox.LostFocus += OnTextBoxLostFocus;
			}

			if (m_valueTextBox != null)
			{
				m_valueTextBox.TextInput += OnValueTextChanging;
				m_valueTextBox.GotFocus += OnTextBoxGotFocus;
				m_valueTextBox.LostFocus += OnTextBoxLostFocus;
			}

			if (m_alphaTextBox != null)
			{
				m_alphaTextBox.TextInput += OnAlphaTextChanging;
				m_alphaTextBox.GotFocus += OnTextBoxGotFocus;
				m_alphaTextBox.LostFocus += OnTextBoxLostFocus;
			}

			if (m_hexTextBox != null)
			{
				m_hexTextBox.TextInput += OnHexTextChanging;
				m_hexTextBox.GotFocus += OnTextBoxGotFocus;
				m_hexTextBox.LostFocus += OnTextBoxLostFocus;
			}

			if (args.NameScope.Get<ComboBoxItem>("PART_RGBComboBoxItem") is ComboBoxItem rgbComboBoxItem)
			{
				rgbComboBoxItem.Content = LocalizedStrings.ContentRGBComboBoxItem;
			}

			if (args.NameScope.Get<ComboBoxItem>("PART_HSVComboBoxItem") is ComboBoxItem hsvComboBoxItem)
			{
				hsvComboBoxItem.Content = LocalizedStrings.ContentHSVComboBoxItem;
			}

			if (m_redLabel != null)
			{
				m_redLabel.Text = LocalizedStrings.TextRedLabel;
			}

			if (m_greenLabel != null)
			{
				m_greenLabel.Text = LocalizedStrings.TextGreenLabel;
			}

			if (m_blueLabel != null)
			{
				m_blueLabel.Text = LocalizedStrings.TextBlueLabel;
			}

			if (m_hueLabel != null)
			{
				m_hueLabel.Text = LocalizedStrings.TextHueLabel;
			}

			if (m_saturationLabel != null)
			{
				m_saturationLabel.Text = LocalizedStrings.TextSaturationLabel;
			}

			if (m_valueLabel != null)
			{
				m_valueLabel.Text = LocalizedStrings.TextValueLabel;
			}

			if (m_alphaLabel != null)
			{
				m_alphaLabel.Text = LocalizedStrings.TextAlphaLabel;
			}

			InitializeColor();
			UpdatePseudoclasses();
		}

		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> args)
		{
			var property = args.Property;

			if (property == ColorProperty)
			{
				OnColorChanged(args);
			}
			else if (property == PreviousColorProperty)
			{
				OnPreviousColorChanged();
			}
			else if (property == IsAlphaEnabledProperty)
			{
				OnIsAlphaEnabledChanged();
			}
			else if (property == MinHueProperty ||
			         property == MaxHueProperty)
			{
				OnMinMaxHueChanged();
			}
			else if (property == MinSaturationProperty ||
			         property == MaxSaturationProperty)
			{
				OnMinMaxSaturationChanged();
			}
			else if (property == MinValueProperty ||
			         property == MaxValueProperty)
			{
				OnMinMaxValueChanged();
			}
			else if (property == ColorSpectrumComponentsProperty)
			{
				OnColorSpectrumComponentsChanged();
			}
			else if (property == IsColorSpectrumVisibleProperty)
			{
				OnIsColorSpectrumVisibleChanged();
			}
		}

		private void OnColorChanged<T>(AvaloniaPropertyChangedEventArgs<T> args)
		{
			// If we're in the process of internally updating the color, then we don't want to respond to the Color property changing,
			// aside from raising the ColorChanged event.
			if (!m_updatingColor)
			{
				var color = Color;

				m_currentRgb = new Rgb(color.R / 255.0, color.G / 255.0, color.B / 255.0);
				m_currentAlpha = color.A / 255.0;
				m_currentHsv = ColorConversion.RgbToHsv(m_currentRgb);
				m_currentHex = GetCurrentHexValue();

				UpdateColorControls(ColorUpdateReason.ColorPropertyChanged);
			}

			var oldColor = args.OldValue.GetValueOrDefault<Color>();
			var newColor = args.NewValue.GetValueOrDefault<Color>();

			if (oldColor.A != newColor.A ||
			    oldColor.R != newColor.R ||
			    oldColor.G != newColor.G ||
			    oldColor.B != newColor.B)
			{
				var colorChangedEventArgs = new ColorChangedEventArgs(oldColor, newColor);
				ColorChanged?.Invoke(this, colorChangedEventArgs);
			}
		}

		private void OnPreviousColorChanged()
		{
			UpdatePseudoclasses();
		}

		private void OnIsAlphaEnabledChanged()
		{
			m_currentHex = GetCurrentHexValue();

			if (m_hexTextBox != null)
			{
				m_updatingControls = true;
				m_hexTextBox.Text = m_currentHex;
				m_updatingControls = false;
			}
		}

		private void OnMinMaxHueChanged()
		{
			var minHue = MinHue;
			var maxHue = MaxHue;

			m_currentHsv.H = Math.Max(minHue, Math.Min(m_currentHsv.H, maxHue));

			UpdateColor(m_currentHsv, ColorUpdateReason.ColorPropertyChanged);
			UpdateThirdDimensionSlider();
		}

		private void OnMinMaxSaturationChanged()
		{
			var minSaturation = MinSaturation;
			var maxSaturation = MaxSaturation;

			m_currentHsv.S = Math.Max((double) minSaturation / 100,
				Math.Min(m_currentHsv.S, (double) maxSaturation / 100));

			UpdateColor(m_currentHsv, ColorUpdateReason.ColorPropertyChanged);
			UpdateThirdDimensionSlider();
		}

		private void OnMinMaxValueChanged()
		{
			var minValue = MinValue;
			var maxValue = MaxValue;

			m_currentHsv.V = Math.Max((double) minValue / 100, Math.Min(m_currentHsv.V, (double) maxValue / 100));

			UpdateColor(m_currentHsv, ColorUpdateReason.ColorPropertyChanged);
			UpdateThirdDimensionSlider();
		}

		private void OnColorSpectrumComponentsChanged()
		{
			UpdateThirdDimensionSlider();
			SetThirdDimensionSliderChannel();
		}

		private void OnIsColorSpectrumVisibleChanged()
		{
			UpdatePseudoclasses();
		}

		private void InitializeColor()
		{
			var color = Color;

			m_currentRgb = new Rgb(color.R / 255.0, color.G / 255.0, color.B / 255.0);
			m_currentHsv = ColorConversion.RgbToHsv(m_currentRgb);
			m_currentAlpha = color.A / 255.0;
			m_currentHex = GetCurrentHexValue();

			SetColorAndUpdateControls(ColorUpdateReason.InitializingColor);
		}

		private void UpdateColor(Rgb rgb, ColorUpdateReason reason)
		{
			m_currentRgb = rgb;
			m_currentHsv = ColorConversion.RgbToHsv(m_currentRgb);
			m_currentHex = GetCurrentHexValue();

			SetColorAndUpdateControls(reason);
		}

		private void UpdateColor(Hsv hsv, ColorUpdateReason reason)
		{
			m_currentHsv = hsv;
			m_currentRgb = ColorConversion.HsvToRgb(hsv);
			m_currentHex = GetCurrentHexValue();

			SetColorAndUpdateControls(reason);
		}

		private void UpdateColor(double alpha, ColorUpdateReason reason)
		{
			m_currentAlpha = alpha;
			m_currentHex = GetCurrentHexValue();

			SetColorAndUpdateControls(reason);
		}

		private void SetColorAndUpdateControls(ColorUpdateReason reason)
		{
			m_updatingColor = true;

			Color = ColorConversion.ColorFromRgba(m_currentRgb, m_currentAlpha);
			UpdateColorControls(reason);

			m_updatingColor = false;
		}

		private void UpdateColorControls(ColorUpdateReason reason)
		{
			// If we're updating the controls internally, we don't want to execute any of the controls'
			// event handlers, because that would then update the color, which would update the color controls,
			// and then we'd be in an infinite loop.
			m_updatingControls = true;

			// We pass in the reason why we're updating the color controls because
			// we don't want to re-update any control that was the cause of this update.
			// For example, if a user selected a color on the ColorSpectrum, then we
			// don't want to update the ColorSpectrum's color based on this change.
			if (reason != ColorUpdateReason.ColorSpectrumColorChanged && m_colorSpectrum != null)
			{
				m_colorSpectrum.HsvColor = new Vector4((float) m_currentHsv.H, (float) m_currentHsv.S,
					(float) m_currentHsv.V, (float) m_currentAlpha);
			}

			if (m_colorPreviewRectangle != null)
			{
				var color = Color;

				m_colorPreviewRectangle.Fill = new SolidColorBrush(color);
			}


			if (reason != ColorUpdateReason.ThirdDimensionSliderChanged && m_thirdDimensionSlider != null)
			{
				UpdateThirdDimensionSlider();
			}

			if (reason != ColorUpdateReason.AlphaSliderChanged && m_alphaSlider != null)
			{
				UpdateAlphaSlider();
			}

			void UpdateTextBoxes()
			{
				if (reason != ColorUpdateReason.RgbTextBoxChanged)
				{
					if (m_redTextBox != null)
					{
						m_redTextBox.Text =
							((byte) Math.Round(m_currentRgb.R * 255)).ToString(CultureInfo.InvariantCulture);
					}

					if (m_greenTextBox != null)
					{
						m_greenTextBox.Text =
							((byte) Math.Round(m_currentRgb.G * 255)).ToString(CultureInfo.InvariantCulture);
					}

					if (m_blueTextBox != null)
					{
						m_blueTextBox.Text =
							((byte) Math.Round(m_currentRgb.B * 255)).ToString(CultureInfo.InvariantCulture);
					}
				}

				if (reason != ColorUpdateReason.HsvTextBoxChanged)
				{
					if (m_hueTextBox != null)
					{
						m_hueTextBox.Text = ((int) Math.Round(m_currentHsv.H)).ToString(CultureInfo.InvariantCulture);
					}

					if (m_saturationTextBox != null)
					{
						m_saturationTextBox.Text =
							((int) Math.Round(m_currentHsv.S * 100)).ToString(CultureInfo.InvariantCulture);
					}

					if (m_valueTextBox != null)
					{
						m_valueTextBox.Text =
							((int) Math.Round(m_currentHsv.V * 100)).ToString(CultureInfo.InvariantCulture);
					}
				}


				if (reason != ColorUpdateReason.AlphaTextBoxChanged)
				{
					if (m_alphaTextBox != null)
					{
						m_alphaTextBox.Text =
							((int) Math.Round(m_currentAlpha * 100)).ToString(CultureInfo.InvariantCulture) + "%";
					}
				}

				if (reason != ColorUpdateReason.HexTextBoxChanged)
				{
					if (m_hexTextBox != null)
					{
						m_hexTextBox.Text = m_currentHex;
					}
				}
			}

			;

			var unknownUWPbugPresentInAvalonia = false;
			// TODO review
			if (!unknownUWPbugPresentInAvalonia)
			{
				// A reentrancy bug with setting TextBox.Text was fixed in RS2,
				// so we can just directly set the TextBoxes' Text property there.
				UpdateTextBoxes();
			}
			else if (unknownUWPbugPresentInAvalonia)
			{
				// Otherwise, we need to post this to the dispatcher to avoid that reentrancy bug.
				Dispatcher.UIThread.InvokeAsync(() =>
				{
					m_updatingControls = true;
					UpdateTextBoxes();
					m_updatingControls = false;
				});
			}

			m_updatingControls = false;
		}

		private void OnColorSpectrumColorChanged(object sender, ColorChangedEventArgs args)
		{
			// If we're updating controls, then this is being raised in response to that,
			// so we'll ignore it.
			if (m_updatingControls)
			{
				return;
			}

			var hsvColor = (Hsv) ((ColorSpectrum) sender).HsvColor;
			UpdateColor(hsvColor, ColorUpdateReason.ColorSpectrumColorChanged);
		}

		private void OnColorSpectrumPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs args)
		{
			if (args.Property == WidthProperty)
			{
				// Since the ColorPicker is arranged vertically, the ColorSpectrum's height can be whatever we want it to be -
				// the width is the limiting factor.  Since we want it to always be a square, we'll set its height to whatever its width is.
				if (args.NewValue is double newValue)
				{
					((ColorSpectrum) sender).Height = newValue;
				}
			}
		}

		private void OnThirdDimensionSliderPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs args)
		{
			if (args.Property == RangeBase.ValueProperty)
			{
				// If we're in the process of updating controls in response to a color change,
				// then we don't want to do anything in response to a control being updated,
				// since otherwise we'll get into an infinite loop of updating.
				if (m_updatingControls)
				{
					return;
				}

				var components = ColorSpectrumComponents;

				var h = m_currentHsv.H;
				var s = m_currentHsv.S;
				var v = m_currentHsv.V;
				var value = ((Slider) sender).Value;

				switch (components)
				{
					case ColorSpectrumComponents.HueValue:
					case ColorSpectrumComponents.ValueHue:
						s = value / 100.0;
						break;

					case ColorSpectrumComponents.HueSaturation:
					case ColorSpectrumComponents.SaturationHue:
						v = value / 100.0;
						break;

					case ColorSpectrumComponents.ValueSaturation:
					case ColorSpectrumComponents.SaturationValue:
						h = value;
						break;
				}

				UpdateColor(new Hsv(h, s, v), ColorUpdateReason.ThirdDimensionSliderChanged);
			}
		}

		private void OnAlphaSliderPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs args)
		{
			if (args.Property == RangeBase.ValueProperty)
			{
				// If we're in the process of updating controls in response to a color change,
				// then we don't want to do anything in response to a control being updated,
				// since otherwise we'll get into an infinite loop of updating.
				if (m_updatingControls)
				{
					return;
				}

				UpdateColor(((Slider) sender).Value / 100.0, ColorUpdateReason.AlphaSliderChanged);
			}
		}

		private void OnMoreButtonChecked(object sender, RoutedEventArgs args)
		{
			m_textEntryGridOpened = true;
			UpdateMoreButton();
		}

		private void OnMoreButtonUnchecked(object sender, RoutedEventArgs args)
		{
			m_textEntryGridOpened = false;
			UpdateMoreButton();
		}

		private void UpdateMoreButton()
		{
			if (m_moreButtonLabel != null)
			{
				m_moreButtonLabel.Text = m_textEntryGridOpened
					? LocalizedStrings.TextMoreButtonLabelExpanded
					: LocalizedStrings.TextMoreButtonLabelCollapsed;
			}
		}

		private void OnTextBoxGotFocus(object sender, GotFocusEventArgs args)
		{
			var textBox = (TextBox) sender;

			m_isFocusedTextBoxValid = true;
			m_previousString = textBox.Text;
		}

		private void OnTextBoxLostFocus(object sender, RoutedEventArgs args)
		{
			var textBox = (TextBox) sender;

			// When a text box loses focus, we want to check whether its contents were valid.
			// If they weren't, then we'll roll back its contents to their last valid value.
			if (!m_isFocusedTextBoxValid)
			{
				textBox.Text = m_previousString;
			}

			// Now that we know that no text box is currently being edited, we'll update all of the color controls
			// in order to clear away any invalid values currently in any text box.
			UpdateColorControls(ColorUpdateReason.ColorPropertyChanged);
		}

		private void OnRgbTextChanging(object sender, TextInputEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			// We'll respond to the text change if the user has entered a valid value.
			// Otherwise, we'll do nothing except mark the text box's contents as invalid.
			var textBox = (TextBox) sender;
			var componentValueHasValue = int.TryParse(textBox.Text, out var componentValue);
			if (!componentValueHasValue ||
			    componentValue < 0 ||
			    componentValue > 255)
			{
				m_isFocusedTextBoxValid = false;
			}
			else
			{
				m_isFocusedTextBoxValid = true;
				UpdateColor(ApplyConstraintsToRgbColor(GetRgbColorFromTextBoxes()),
					ColorUpdateReason.RgbTextBoxChanged);
			}
		}

		private void OnHueTextChanging(object sender, TextInputEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			// We'll respond to the text change if the user has entered a valid value.
			// Otherwise, we'll do nothing except mark the text box's contents as invalid.
			var textBox = (TextBox) sender;
			var hueValueHasValue = int.TryParse(textBox.Text, out var hueValue);
			if (!hueValueHasValue ||
			    hueValue < MinHue ||
			    hueValue > MaxHue)
			{
				m_isFocusedTextBoxValid = false;
			}
			else
			{
				m_isFocusedTextBoxValid = true;
				UpdateColor(GetHsvColorFromTextBoxes(), ColorUpdateReason.HsvTextBoxChanged);
			}
		}

		private void OnSaturationTextChanging(object sender, TextInputEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			// We'll respond to the text change if the user has entered a valid value.
			// Otherwise, we'll do nothing except mark the text box's contents as invalid.
			var textBox = (TextBox) sender;
			var saturationValueHasValue = int.TryParse(textBox.Text, out var saturationValue);
			if (!saturationValueHasValue ||
			    saturationValue < (long) MinSaturation ||
			    saturationValue > (long) MaxSaturation)
			{
				m_isFocusedTextBoxValid = false;
			}
			else
			{
				m_isFocusedTextBoxValid = true;
				UpdateColor(GetHsvColorFromTextBoxes(), ColorUpdateReason.HsvTextBoxChanged);
			}
		}

		private void OnValueTextChanging(object sender, TextInputEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			// We'll respond to the text change if the user has entered a valid value.
			// Otherwise, we'll do nothing except mark the text box's contents as invalid.
			var textBox = (TextBox) sender;
			var valueHasValue = int.TryParse(textBox.Text, out var value);
			if (!valueHasValue ||
			    value < (long) MinValue ||
			    value > (long) MaxValue)
			{
				m_isFocusedTextBoxValid = false;
			}
			else
			{
				m_isFocusedTextBoxValid = true;
				UpdateColor(GetHsvColorFromTextBoxes(), ColorUpdateReason.HsvTextBoxChanged);
			}
		}

		private void OnAlphaTextChanging(object sender, TextInputEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			if (m_alphaTextBox != null)
			{
				// If the user hasn't entered a %, we'll do that for them, keeping the cursor
				// where it was before.
				// m_alphaTextBox.SelectionStart + m_alphaTextBox.SelectionLength
				var cursorPosition = m_alphaTextBox.CaretIndex;

				var alphaTextBoxText = m_alphaTextBox.Text;

				if (alphaTextBoxText.Length == 0 || alphaTextBoxText[alphaTextBoxText.Length - 1] != '%')
				{
					m_alphaTextBox.Text = alphaTextBoxText += "%";
					m_alphaTextBox.CaretIndex = cursorPosition;
				}

				// We'll respond to the text change if the user has entered a valid value.
				// Otherwise, we'll do nothing except mark the text box's contents as invalid.
				var alphaString = alphaTextBoxText.Substring(0, alphaTextBoxText.Length - 1);
				var alphaValueHasValue = int.TryParse(alphaString, out var alphaValue);
				if (!alphaValueHasValue || alphaValue < 0 || alphaValue > 100)
				{
					m_isFocusedTextBoxValid = false;
				}
				else
				{
					m_isFocusedTextBoxValid = true;
					UpdateColor(alphaValue / 100.0, ColorUpdateReason.AlphaTextBoxChanged);
				}
			}
		}

		private void OnHexTextChanging(object sender, TextInputEventArgs args)
		{
			// If we're in the process of updating controls in response to a color change,
			// then we don't want to do anything in response to a control being updated,
			// since otherwise we'll get into an infinite loop of updating.
			if (m_updatingControls)
			{
				return;
			}

			var hexTextBox = (TextBox) sender;
			var hexTextBoxText = hexTextBox.Text;

			// If the user hasn't entered a #, we'll do that for them, keeping the cursor
			// where it was before.
			if (hexTextBoxText.Length == 0 || hexTextBoxText[0] != '#')
			{
				hexTextBox.Text = hexTextBoxText = "#" + hexTextBoxText;
				// TODO should it use saved before length?
				hexTextBox.CaretIndex = hexTextBoxText.Length;
			}

			// We'll respond to the text change if the user has entered a valid value.
			// Otherwise, we'll do nothing except mark the text box's contents as invalid.
			var isAlphaEnabled = IsAlphaEnabled;
			if (Color.TryParse(hexTextBoxText, out var parsedColor))
			{
				var rgbValue = new Rgb(parsedColor.R / 255.0, parsedColor.G / 255.0, parsedColor.B / 255.0);
				var alphaValue = parsedColor.A / 255.0;
				if (!isAlphaEnabled)
				{
					alphaValue = 1.0;
				}

				m_isFocusedTextBoxValid = true;
				UpdateColor(ApplyConstraintsToRgbColor(rgbValue), ColorUpdateReason.HexTextBoxChanged);
				UpdateColor(alphaValue, ColorUpdateReason.HexTextBoxChanged);
			}
			else
			{
				m_isFocusedTextBoxValid = false;
			}
		}

		private void UpdatePseudoclasses()
		{
			PseudoClasses.Set(":has-previous-color", PreviousColor.HasValue);
			PseudoClasses.Set(":spectrum-visible", IsColorSpectrumVisible);
		}

		private Rgb GetRgbColorFromTextBoxes()
		{
			_ = int.TryParse(m_redTextBox?.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var redValue);
			_ = int.TryParse(m_greenTextBox?.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var greenValue);
			_ = int.TryParse(m_blueTextBox?.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var blueValue);

			return new Rgb(redValue / 255.0, greenValue / 255.0, blueValue / 255.0);
		}

		private Hsv GetHsvColorFromTextBoxes()
		{
			_ = int.TryParse(m_hueTextBox?.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var hueValue);
			_ = int.TryParse(m_saturationTextBox?.Text, NumberStyles.None, CultureInfo.InvariantCulture,
				out var saturationValue);
			_ = int.TryParse(m_valueTextBox?.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var valueValue);

			return new Hsv(hueValue, saturationValue / 100.0, valueValue / 100.0);
		}

		private string GetCurrentHexValue()
		{
			var colorHex = ColorConversion.ColorFromRgba(m_currentRgb, m_currentAlpha).ToUint32()
				.ToString("x8", CultureInfo.InvariantCulture);
			if (!IsAlphaEnabled)
			{
				colorHex = colorHex.Substring(2);
			}

			return "#" + colorHex;
		}

		private Rgb ApplyConstraintsToRgbColor(Rgb rgb)
		{
			double minHue = MinHue;
			double maxHue = MaxHue;
			var minSaturation = MinSaturation / 100.0;
			var maxSaturation = MaxSaturation / 100.0;
			var minValue = MinValue / 100.0;
			var maxValue = MaxValue / 100.0;

			var hsv = ColorConversion.RgbToHsv(rgb);

			hsv.H = Math.Min(Math.Max(hsv.H, minHue), maxHue);
			hsv.S = Math.Min(Math.Max(hsv.S, minSaturation), maxSaturation);
			hsv.V = Math.Min(Math.Max(hsv.V, minValue), maxValue);

			return ColorConversion.HsvToRgb(hsv);
		}

		private void UpdateThirdDimensionSlider()
		{
			if (m_thirdDimensionSlider == null
			    || m_thirdDimensionSliderGradientBrush == null)
			{
				return;
			}

			// Since the slider changes only one color dimension, we can use a LinearGradientBrush
			// for its background instead of needing to manually set pixels ourselves.
			// We'll have the gradient go between the minimum and maximum values in the case where
			// the slider handles saturation or value, or in the case where it handles hue,
			// we'll have it go between red, yellow, green, cyan, blue, and purple, in that order.
			m_thirdDimensionSliderGradientBrush.GradientStops.Clear();

			switch (ColorSpectrumComponents)
			{
				case ColorSpectrumComponents.HueValue:
				case ColorSpectrumComponents.ValueHue:
				{
					var minSaturation = MinSaturation;
					var maxSaturation = MaxSaturation;

					m_thirdDimensionSlider.Minimum = minSaturation;
					m_thirdDimensionSlider.Maximum = maxSaturation;
					m_thirdDimensionSlider.Value = m_currentHsv.S * 100;

					// If MinSaturation >= MaxSaturation, then by convention MinSaturation is the only value
					// that the slider can take.
					if (minSaturation >= maxSaturation)
					{
						maxSaturation = minSaturation;
					}

					AddGradientStop(m_thirdDimensionSliderGradientBrush, 0.0,
						new Hsv(m_currentHsv.H, minSaturation / 100.0, 1.0), 1.0);
					AddGradientStop(m_thirdDimensionSliderGradientBrush, 1.0,
						new Hsv(m_currentHsv.H, maxSaturation / 100.0, 1.0), 1.0);
				}
					break;

				case ColorSpectrumComponents.HueSaturation:
				case ColorSpectrumComponents.SaturationHue:
				{
					var minValue = MinValue;
					var maxValue = MaxValue;

					m_thirdDimensionSlider.Minimum = minValue;
					m_thirdDimensionSlider.Maximum = maxValue;
					m_thirdDimensionSlider.Value = m_currentHsv.V * 100;

					// If MinValue >= MaxValue, then by convention MinValue is the only value
					// that the slider can take.
					if (minValue >= maxValue)
					{
						maxValue = minValue;
					}

					AddGradientStop(m_thirdDimensionSliderGradientBrush, 0.0,
						new Hsv(m_currentHsv.H, m_currentHsv.S, minValue / 100.0), 1.0);
					AddGradientStop(m_thirdDimensionSliderGradientBrush, 1.0,
						new Hsv(m_currentHsv.H, m_currentHsv.S, maxValue / 100.0), 1.0);
				}
					break;

				case ColorSpectrumComponents.ValueSaturation:
				case ColorSpectrumComponents.SaturationValue:
				{
					var minHue = MinHue;
					var maxHue = MaxHue;

					m_thirdDimensionSlider.Minimum = minHue;
					m_thirdDimensionSlider.Maximum = maxHue;
					m_thirdDimensionSlider.Value = m_currentHsv.H;

					// If MinHue >= MaxHue, then by convention MinHue is the only value
					// that the slider can take.
					if (minHue >= maxHue)
					{
						maxHue = minHue;
					}

					var minOffset = minHue / 359.0;
					var maxOffset = maxHue / 359.0;

					// With unclamped hue values, we have six different gradient stops, corresponding to red, yellow, green, cyan, blue, and purple.
					// However, with clamped hue values, we may not need all of those gradient stops.
					// We know we need a gradient stop at the start and end corresponding to the min and max values for hue,
					// and then in the middle, we'll add any gradient stops corresponding to the hue of those six pure colors that exist
					// between the min and max hue.
					AddGradientStop(m_thirdDimensionSliderGradientBrush, 0.0, new Hsv(minHue, 1.0, 1.0), 1.0);

					for (var sextant = 1; sextant <= 5; sextant++)
					{
						var offset = sextant / 6.0;

						if (minOffset < offset && maxOffset > offset)
						{
							AddGradientStop(m_thirdDimensionSliderGradientBrush,
								(offset - minOffset) / (maxOffset - minOffset), new Hsv(60.0 * sextant, 1.0, 1.0), 1.0);
						}
					}

					AddGradientStop(m_thirdDimensionSliderGradientBrush, 1.0, new Hsv(maxHue, 1.0, 1.0), 1.0);
				}
					break;
			}
		}

		private void SetThirdDimensionSliderChannel()
		{
			if (m_thirdDimensionSlider != null)
			{
				switch (ColorSpectrumComponents)
				{
					case ColorSpectrumComponents.ValueSaturation:
					case ColorSpectrumComponents.SaturationValue:
						m_thirdDimensionSlider.ColorChannel = ColorPickerHsvChannel.Hue;
						// TODO
						//AutomationProperties::SetName(m_thirdDimensionSlider, ResourceAccessor::GetLocalizedStringResource(SR_AutomationNameHueSlider));
						break;

					case ColorSpectrumComponents.HueValue:
					case ColorSpectrumComponents.ValueHue:
						m_thirdDimensionSlider.ColorChannel = ColorPickerHsvChannel.Saturation;
						// TODO
						//AutomationProperties::SetName(m_thirdDimensionSlider, ResourceAccessor::GetLocalizedStringResource(SR_AutomationNameSaturationSlider));
						break;

					case ColorSpectrumComponents.HueSaturation:
					case ColorSpectrumComponents.SaturationHue:
						m_thirdDimensionSlider.ColorChannel = ColorPickerHsvChannel.Value;
						// TODO
						//AutomationProperties::SetName(m_thirdDimensionSlider, ResourceAccessor::GetLocalizedStringResource(SR_AutomationNameValueSlider));
						break;
				}
			}
		}

		private void UpdateAlphaSlider()
		{
			if (m_alphaSlider == null)
			{
				return;
			}

			m_alphaSlider.Value = m_currentAlpha * 100;
		}

		private static void AddGradientStop(LinearGradientBrush brush, double offset, Hsv hsvColor, double alpha)
		{
			var rgbColor = ColorConversion.HsvToRgb(hsvColor);

			var color = Color.FromArgb(
				(byte) Math.Round(alpha * 255),
				(byte) Math.Round(rgbColor.R * 255),
				(byte) Math.Round(rgbColor.G * 255),
				(byte) Math.Round(rgbColor.B * 255));
			var stop = new GradientStop(color, offset);

			brush.GradientStops.Add(stop);
		}
	}
}