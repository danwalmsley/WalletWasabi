using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Utilities;

namespace AvaloniaWinUI.ColorPicker
{
	public partial class ColorSpectrum : TemplatedControl
	{
		private ColorSpectrumComponents m_componentsFromLastBitmapCreation = ColorSpectrumComponents.HueSaturation;
		private List<Hsv> m_hsvValues;
		private IBitmap? m_hueBlueBitmap;
		private IBitmap? m_hueCyanBitmap;
		private IBitmap? m_hueGreenBitmap;
		private IBitmap? m_huePurpleBitmap;

		private IBitmap? m_hueRedBitmap;
		private IBitmap? m_hueYellowBitmap;
		private double m_imageHeightFromLastBitmapCreation;
		private double m_imageWidthFromLastBitmapCreation;
		private Canvas? m_inputTarget;
		private bool m_isPointerOver;
		private bool m_isPointerPressed;

		// XAML elements
		private Grid? m_layoutRoot;
		private int m_maxHueFromLastBitmapCreation;
		private int m_maxSaturationFromLastBitmapCreation;
		private int m_maxValueFromLastBitmapCreation;
		private int m_minHueFromLastBitmapCreation;
		private int m_minSaturationFromLastBitmapCreation;
		private int m_minValueFromLastBitmapCreation;

		private Color m_oldColor = Colors.White;
		private IBitmap? m_saturationMaximumBitmap;
		private IBitmap? m_saturationMinimumBitmap;
		private Control? m_selectionEllipse;
		private Panel? m_selectionEllipsePanel;

		// Fields used by UpdateEllipse() to ensure that it's using the data
		// associated with the last call to CreateBitmapsAndColorMap(),
		// in order to function properly while the asynchronous bitmap creation
		// is in progress.
		private ColorSpectrumShape m_shapeFromLastBitmapCreation = ColorSpectrumShape.Box;
		private bool m_shouldShowLargeSelection;
		private Grid? m_sizingGrid;
		private Ellipse? m_spectrumEllipse;
		private Ellipse? m_spectrumOverlayEllipse;
		private Rectangle? m_spectrumOverlayRectangle;
		private Rectangle? m_spectrumRectangle;
		private bool m_updatingColor;
		private bool m_updatingHsvColor;
		private IBitmap? m_valueBitmap;

		public ColorSpectrum()
		{
			m_shapeFromLastBitmapCreation = Shape;
			m_componentsFromLastBitmapCreation = Components;
			m_minHueFromLastBitmapCreation = MinHue;
			m_maxHueFromLastBitmapCreation = MaxHue;
			m_minSaturationFromLastBitmapCreation = MinSaturation;
			m_maxSaturationFromLastBitmapCreation = MaxSaturation;
			m_minValueFromLastBitmapCreation = MinValue;
			m_maxValueFromLastBitmapCreation = MaxValue;

			m_hsvValues = new List<Hsv>();
		}

		protected override void OnApplyTemplate(TemplateAppliedEventArgs args)
		{
			base.OnApplyTemplate(args);

			m_layoutRoot = args.NameScope.Find<Grid>("PART_LayoutRoot");
			m_sizingGrid = args.NameScope.Find<Grid>("PART_SizingGrid");
			m_spectrumRectangle = args.NameScope.Find<Rectangle>("PART_SpectrumRectangle");
			m_spectrumEllipse = args.NameScope.Find<Ellipse>("PART_SpectrumEllipse");
			m_spectrumOverlayRectangle = args.NameScope.Find<Rectangle>("PART_SpectrumOverlayRectangle");
			m_spectrumOverlayEllipse = args.NameScope.Find<Ellipse>("PART_SpectrumOverlayEllipse");
			m_inputTarget = args.NameScope.Find<Canvas>("PART_InputTarget");
			m_selectionEllipsePanel = args.NameScope.Find<Panel>("PART_SelectionEllipsePanel");
			m_selectionEllipse = args.NameScope.Find<Control>("PART_SelectionEllipse");
			if (m_layoutRoot != null)
			{
				m_layoutRoot.GetObservable(BoundsProperty).Subscribe(_ => CreateBitmapsAndColorMap());
			}

			if (m_inputTarget != null)
			{
				m_inputTarget.PointerEnter += OnInputTargetPointerEntered;
				m_inputTarget.PointerLeave += OnInputTargetPointerExited;
				m_inputTarget.PointerPressed += OnInputTargetPointerPressed;
				m_inputTarget.PointerMoved += OnInputTargetPointerMoved;
				m_inputTarget.PointerReleased += OnInputTargetPointerReleased;
			}

			if (ToolTip.GetTip(m_selectionEllipse) is ToolTip selectionEllipseTooltip)
			{
				selectionEllipseTooltip.Content = ColorHelpers.ToDisplayName(Color);
			}

			if (m_hsvValues.Count == 0)
			{
				CreateBitmapsAndColorMap();
			}

			UpdateEllipse();
			UpdatePseudoclasses();
		}

		protected override void OnKeyDown(KeyEventArgs args)
		{
			if (args.Key != Key.Left &&
			    args.Key != Key.Right &&
			    args.Key != Key.Up &&
			    args.Key != Key.Down)
			{
				base.OnKeyDown(args);
				return;
			}

			var isControlDown = args.KeyModifiers.HasFlag(KeyModifiers.Control);

			var incrementChannel = ColorPickerHsvChannel.Hue;

			if (args.Key == Key.Left ||
			    args.Key == Key.Right)
			{
				switch (Components)
				{
					case ColorSpectrumComponents.HueSaturation:
					case ColorSpectrumComponents.HueValue:
						incrementChannel = ColorPickerHsvChannel.Hue;
						break;

					case ColorSpectrumComponents.SaturationHue:
					case ColorSpectrumComponents.SaturationValue:
						incrementChannel = ColorPickerHsvChannel.Saturation;
						break;

					case ColorSpectrumComponents.ValueHue:
					case ColorSpectrumComponents.ValueSaturation:
						incrementChannel = ColorPickerHsvChannel.Value;
						break;
				}
			}
			else if (args.Key == Key.Up ||
			         args.Key == Key.Down)
			{
				switch (Components)
				{
					case ColorSpectrumComponents.SaturationHue:
					case ColorSpectrumComponents.ValueHue:
						incrementChannel = ColorPickerHsvChannel.Hue;
						break;

					case ColorSpectrumComponents.HueSaturation:
					case ColorSpectrumComponents.ValueSaturation:
						incrementChannel = ColorPickerHsvChannel.Saturation;
						break;

					case ColorSpectrumComponents.HueValue:
					case ColorSpectrumComponents.SaturationValue:
						incrementChannel = ColorPickerHsvChannel.Value;
						break;
				}
			}

			double minBound = 0;
			double maxBound = 0;

			switch (incrementChannel)
			{
				case ColorPickerHsvChannel.Hue:
					minBound = MinHue;
					maxBound = MaxHue;
					break;

				case ColorPickerHsvChannel.Saturation:
					minBound = MinSaturation;
					maxBound = MaxSaturation;
					break;

				case ColorPickerHsvChannel.Value:
					minBound = MinValue;
					maxBound = MaxValue;
					break;
			}

			// The order of saturation and value in the spectrum is reversed - the max value is at the bottom while the min value is at the top -
			// so we want left and up to be lower for hue, but higher for saturation and value.
			// This will ensure that the icon always moves in the direction of the key press.
			var direction =
				incrementChannel == ColorPickerHsvChannel.Hue && (args.Key == Key.Left || args.Key == Key.Up) ||
				incrementChannel != ColorPickerHsvChannel.Hue && (args.Key == Key.Right || args.Key == Key.Down)
					? IncrementDirection.Lower
					: IncrementDirection.Higher;

			var amount = isControlDown ? IncrementAmount.Large : IncrementAmount.Small;

			var hsvColor = HsvColor;
			var incrementedColor = ColorHelpers.IncrementColorChannel(new Hsv(hsvColor.X, hsvColor.Y, hsvColor.Z),
				incrementChannel, direction, amount, true /* shouldWrap */, minBound, maxBound);
			UpdateColor(incrementedColor);
			args.Handled = true;
		}

		protected override void OnGotFocus(GotFocusEventArgs args)
		{
			if (m_selectionEllipse != null)
			{
				ToolTip.SetIsOpen(m_selectionEllipse, true);
			}

			UpdatePseudoclasses();
		}

		protected override void OnLostFocus(RoutedEventArgs args)
		{
			if (m_selectionEllipse != null)
			{
				ToolTip.SetIsOpen(m_selectionEllipse, false);
			}

			UpdatePseudoclasses();
		}

		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> args)
		{
			if (args.Property == ColorProperty)
			{
				OnColorChanged(args);
			}
			else if (args.Property == HsvColorProperty)
			{
				OnHsvColorChanged();
			}
			else if (args.Property == MinHueProperty ||
			         args.Property == MaxHueProperty)
			{
				OnMinMaxHueChanged();
			}
			else if (args.Property == MinSaturationProperty ||
			         args.Property == MaxSaturationProperty)
			{
				OnMinMaxSaturationChanged();
			}
			else if (args.Property == MinValueProperty ||
			         args.Property == MaxValueProperty)
			{
				OnMinMaxValueChanged();
			}
			else if (args.Property == ShapeProperty)
			{
				OnShapeChanged();
			}
			else if (args.Property == ComponentsProperty)
			{
				OnComponentsChanged();
			}
		}

		private void OnColorChanged<T>(AvaloniaPropertyChangedEventArgs<T> args)
		{
			// If we're in the process of internally updating the color, then we don't want to respond to the Color property changing.
			if (!m_updatingColor)
			{
				var color = args.NewValue.GetValueOrDefault<Color>();

				m_updatingHsvColor = true;
				var newHsv = ColorConversion.RgbToHsv(new Rgb(color.R / 255.0, color.G / 255.0, color.B / 255.0));
				HsvColor = new Vector4((float) newHsv.H, (float) newHsv.S, (float) newHsv.V, (float) (color.A / 255.0));
				m_updatingHsvColor = false;

				UpdateEllipse();
				UpdateBitmapSources();
			}

			m_oldColor = args.OldValue.GetValueOrDefault<Color>();
		}

		private void OnHsvColorChanged()
		{
			// If we're in the process of internally updating the HSV color, then we don't want to respond to the HsvColor property changing.
			if (!m_updatingHsvColor)
			{
				SetColor();
			}
		}

		private void SetColor()
		{
			var hsvColor = HsvColor;

			m_updatingColor = true;

			var newRgb = ColorConversion.HsvToRgb((Hsv) hsvColor);

			Color = ColorConversion.ColorFromRgba(newRgb, hsvColor.Z);

			m_updatingColor = false;

			UpdateEllipse();
			UpdateBitmapSources();
			RaiseColorChanged();
		}

		private void RaiseColorChanged()
		{
			var newColor = Color;

			if (m_oldColor.A != newColor.A ||
			    m_oldColor.R != newColor.R ||
			    m_oldColor.G != newColor.G ||
			    m_oldColor.B != newColor.B)
			{
				var colorChangedEventArgs = new ColorChangedEventArgs(m_oldColor, newColor);
				ColorChanged?.Invoke(this, colorChangedEventArgs);

				if (m_selectionEllipse != null
				    && ToolTip.GetTip(m_selectionEllipse) is ToolTip selectionEllipseTooltip)
				{
					selectionEllipseTooltip.Content = ColorHelpers.ToDisplayName(Color);
				}
			}
		}

		private void OnMinMaxHueChanged()
		{
			var components = Components;

			// If hue is one of the axes in the spectrum bitmap, then we'll need to regenerate it
			// if the maximum or minimum value has changed.
			if (components != ColorSpectrumComponents.SaturationValue &&
			    components != ColorSpectrumComponents.ValueSaturation)
			{
				CreateBitmapsAndColorMap();
			}
		}

		private void OnMinMaxSaturationChanged()
		{
			var components = Components;

			// If value is one of the axes in the spectrum bitmap, then we'll need to regenerate it
			// if the maximum or minimum value has changed.
			if (components != ColorSpectrumComponents.HueValue &&
			    components != ColorSpectrumComponents.ValueHue)
			{
				CreateBitmapsAndColorMap();
			}
		}

		private void OnMinMaxValueChanged()
		{
			var components = Components;

			// If value is one of the axes in the spectrum bitmap, then we'll need to regenerate it
			// if the maximum or minimum value has changed.
			if (components != ColorSpectrumComponents.HueSaturation &&
			    components != ColorSpectrumComponents.SaturationHue)
			{
				CreateBitmapsAndColorMap();
			}
		}

		private void OnShapeChanged()
		{
			CreateBitmapsAndColorMap();
		}

		private void OnComponentsChanged()
		{
			CreateBitmapsAndColorMap();
		}

		private void UpdatePseudoclasses()
		{
			PseudoClasses.Set(":pressed", m_isPointerPressed);
			PseudoClasses.Set(":pointerover", m_isPointerOver);
			PseudoClasses.Set(":touch", m_shouldShowLargeSelection);

			PseudoClasses.Set(":box", m_shapeFromLastBitmapCreation == ColorSpectrumShape.Box);
			PseudoClasses.Set(":ring", m_shapeFromLastBitmapCreation == ColorSpectrumShape.Ring);

			PseudoClasses.Set(":light-selector", SelectionEllipseShouldBeLight());
		}

		private void UpdateColor(Hsv newHsv)
		{
			m_updatingColor = true;
			m_updatingHsvColor = true;

			var newRgb = ColorConversion.HsvToRgb(newHsv);
			var alpha = HsvColor.Z;

			Color = ColorConversion.ColorFromRgba(newRgb, alpha);
			HsvColor = new Vector4((float) newHsv.H, (float) newHsv.S, (float) newHsv.V, alpha);

			UpdateEllipse();
			UpdatePseudoclasses();

			m_updatingHsvColor = false;
			m_updatingColor = false;

			RaiseColorChanged();
		}

		private void UpdateColorFromPoint(PointerPoint point)
		{
			// If we haven't initialized our HSV value array yet, then we should just ignore any user input -
			// we don't yet know what to do with it.
			if (!m_hsvValues.Any())
			{
				return;
			}

			var xPosition = point.Position.X;
			var yPosition = point.Position.Y;
			var radius = Math.Min(m_imageWidthFromLastBitmapCreation, m_imageHeightFromLastBitmapCreation) / 2;
			var distanceFromRadius = Math.Sqrt(Math.Pow(xPosition - radius, 2) + Math.Pow(yPosition - radius, 2));

			var shape = Shape;

			// If the point is outside the circle, we should bring it back into the circle.
			if (distanceFromRadius > radius && shape == ColorSpectrumShape.Ring)
			{
				xPosition = radius / distanceFromRadius * (xPosition - radius) + radius;
				yPosition = radius / distanceFromRadius * (yPosition - radius) + radius;
			}

			// Now we need to find the index into the array of HSL values at each point in the spectrum m_image.
			var x = (int) Math.Round(xPosition);
			var y = (int) Math.Round(yPosition);
			var width = (int) Math.Round(m_imageWidthFromLastBitmapCreation);

			if (x < 0)
			{
				x = 0;
			}
			else if (x >= m_imageWidthFromLastBitmapCreation)
			{
				x = (int) Math.Round(m_imageWidthFromLastBitmapCreation) - 1;
			}

			if (y < 0)
			{
				y = 0;
			}
			else if (y >= m_imageHeightFromLastBitmapCreation)
			{
				y = (int) Math.Round(m_imageHeightFromLastBitmapCreation) - 1;
			}

			// The gradient image contains two dimensions of HSL information, but not the third.
			// We should keep the third where it already was.
			var hsvAtPoint = m_hsvValues[y * width + x];

			var components = Components;
			var hsvColor = (Hsv) HsvColor;

			switch (components)
			{
				case ColorSpectrumComponents.HueValue:
				case ColorSpectrumComponents.ValueHue:
					hsvAtPoint.S = hsvColor.S;
					break;

				case ColorSpectrumComponents.HueSaturation:
				case ColorSpectrumComponents.SaturationHue:
					hsvAtPoint.V = hsvColor.V;
					break;

				case ColorSpectrumComponents.ValueSaturation:
				case ColorSpectrumComponents.SaturationValue:
					hsvAtPoint.H = hsvColor.H;
					break;
			}

			UpdateColor(hsvAtPoint);
		}

		private void UpdateEllipse()
		{
			if (m_selectionEllipsePanel == null)
			{
				return;
			}

			// If we don't have an image size yet, we shouldn't be showing the ellipse.
			if (m_imageWidthFromLastBitmapCreation == 0 ||
			    m_imageHeightFromLastBitmapCreation == 0)
			{
				m_selectionEllipsePanel.IsVisible = false;
				return;
			}

			m_selectionEllipsePanel.IsVisible = true;

			double xPosition;
			double yPosition;

			var hsvColor = (Hsv) HsvColor;

			hsvColor.H =
				MathUtilities.Clamp(hsvColor.H, m_minHueFromLastBitmapCreation, m_maxHueFromLastBitmapCreation);
			hsvColor.S = MathUtilities.Clamp(hsvColor.S, m_minSaturationFromLastBitmapCreation / 100.0f,
				m_maxSaturationFromLastBitmapCreation / 100.0f);
			hsvColor.V = MathUtilities.Clamp(hsvColor.V, m_minValueFromLastBitmapCreation / 100.0f,
				m_maxValueFromLastBitmapCreation / 100.0f);

			if (m_shapeFromLastBitmapCreation == ColorSpectrumShape.Box)
			{
				double xPercent = 0;
				double yPercent = 0;

				var hPercent = (hsvColor.H - m_minHueFromLastBitmapCreation) /
				               (m_maxHueFromLastBitmapCreation - m_minHueFromLastBitmapCreation);
				var sPercent = (hsvColor.S * 100.0 - m_minSaturationFromLastBitmapCreation) /
				               (m_maxSaturationFromLastBitmapCreation - m_minSaturationFromLastBitmapCreation);
				var vPercent = (hsvColor.V * 100.0 - m_minValueFromLastBitmapCreation) /
				               (m_maxValueFromLastBitmapCreation - m_minValueFromLastBitmapCreation);

				// In the case where saturation was an axis in the spectrum with hue, or value is an axis, full stop,
				// we inverted the direction of that axis in order to put more hue on the outside of the ring,
				// so we need to do similarly here when positioning the ellipse.
				if (m_componentsFromLastBitmapCreation == ColorSpectrumComponents.HueSaturation ||
				    m_componentsFromLastBitmapCreation == ColorSpectrumComponents.SaturationHue)
				{
					sPercent = 1 - sPercent;
				}
				else
				{
					vPercent = 1 - vPercent;
				}

				switch (m_componentsFromLastBitmapCreation)
				{
					case ColorSpectrumComponents.HueValue:
						xPercent = hPercent;
						yPercent = vPercent;
						break;

					case ColorSpectrumComponents.HueSaturation:
						xPercent = hPercent;
						yPercent = sPercent;
						break;

					case ColorSpectrumComponents.ValueHue:
						xPercent = vPercent;
						yPercent = hPercent;
						break;

					case ColorSpectrumComponents.ValueSaturation:
						xPercent = vPercent;
						yPercent = sPercent;
						break;

					case ColorSpectrumComponents.SaturationHue:
						xPercent = sPercent;
						yPercent = hPercent;
						break;

					case ColorSpectrumComponents.SaturationValue:
						xPercent = sPercent;
						yPercent = vPercent;
						break;
				}

				xPosition = m_imageWidthFromLastBitmapCreation * xPercent;
				yPosition = m_imageHeightFromLastBitmapCreation * yPercent;
			}
			else
			{
				double thetaValue = 0;
				double rValue = 0;

				var hThetaValue =
					m_maxHueFromLastBitmapCreation != m_minHueFromLastBitmapCreation
						? 360 * (hsvColor.H - m_minHueFromLastBitmapCreation) /
						  (m_maxHueFromLastBitmapCreation - m_minHueFromLastBitmapCreation)
						: 0;
				var sThetaValue =
					m_maxSaturationFromLastBitmapCreation != m_minSaturationFromLastBitmapCreation
						? 360 * (hsvColor.S * 100.0 - m_minSaturationFromLastBitmapCreation) /
						  (m_maxSaturationFromLastBitmapCreation - m_minSaturationFromLastBitmapCreation)
						: 0;
				var vThetaValue =
					m_maxValueFromLastBitmapCreation != m_minValueFromLastBitmapCreation
						? 360 * (hsvColor.V * 100.0 - m_minValueFromLastBitmapCreation) /
						  (m_maxValueFromLastBitmapCreation - m_minValueFromLastBitmapCreation)
						: 0;
				var hRValue = m_maxHueFromLastBitmapCreation != m_minHueFromLastBitmapCreation
					? (hsvColor.H - m_minHueFromLastBitmapCreation) /
					(m_maxHueFromLastBitmapCreation - m_minHueFromLastBitmapCreation) - 1
					: 0;
				var sRValue = m_maxSaturationFromLastBitmapCreation != m_minSaturationFromLastBitmapCreation
					? (hsvColor.S * 100.0 - m_minSaturationFromLastBitmapCreation) /
					(m_maxSaturationFromLastBitmapCreation - m_minSaturationFromLastBitmapCreation) - 1
					: 0;
				var vRValue = m_maxValueFromLastBitmapCreation != m_minValueFromLastBitmapCreation
					? (hsvColor.V * 100.0 - m_minValueFromLastBitmapCreation) /
					(m_maxValueFromLastBitmapCreation - m_minValueFromLastBitmapCreation) - 1
					: 0;

				// In the case where saturation was an axis in the spectrum with hue, or value is an axis, full stop,
				// we inverted the direction of that axis in order to put more hue on the outside of the ring,
				// so we need to do similarly here when positioning the ellipse.
				if (m_componentsFromLastBitmapCreation == ColorSpectrumComponents.HueSaturation ||
				    m_componentsFromLastBitmapCreation == ColorSpectrumComponents.ValueHue)
				{
					sThetaValue = 360 - sThetaValue;
					sRValue = -sRValue - 1;
				}
				else
				{
					vThetaValue = 360 - vThetaValue;
					vRValue = -vRValue - 1;
				}

				switch (m_componentsFromLastBitmapCreation)
				{
					case ColorSpectrumComponents.HueValue:
						thetaValue = hThetaValue;
						rValue = vRValue;
						break;

					case ColorSpectrumComponents.HueSaturation:
						thetaValue = hThetaValue;
						rValue = sRValue;
						break;

					case ColorSpectrumComponents.ValueHue:
						thetaValue = vThetaValue;
						rValue = hRValue;
						break;

					case ColorSpectrumComponents.ValueSaturation:
						thetaValue = vThetaValue;
						rValue = sRValue;
						break;

					case ColorSpectrumComponents.SaturationHue:
						thetaValue = sThetaValue;
						rValue = hRValue;
						break;

					case ColorSpectrumComponents.SaturationValue:
						thetaValue = sThetaValue;
						rValue = vRValue;
						break;
				}

				var radius = Math.Min(m_imageWidthFromLastBitmapCreation, m_imageHeightFromLastBitmapCreation) / 2;

				xPosition = Math.Cos(thetaValue * Math.PI / 180 + Math.PI) * radius * rValue + radius;
				yPosition = Math.Sin(thetaValue * Math.PI / 180 + Math.PI) * radius * rValue + radius;
			}

			Canvas.SetLeft(m_selectionEllipsePanel, xPosition - m_selectionEllipsePanel.Width / 2);
			Canvas.SetTop(m_selectionEllipsePanel, yPosition - m_selectionEllipsePanel.Height / 2);

			UpdatePseudoclasses();
		}

		private void OnInputTargetPointerEntered(object sender, PointerEventArgs args)
		{
			m_isPointerOver = true;
			UpdatePseudoclasses();
			args.Handled = true;
		}

		private void OnInputTargetPointerExited(object sender, PointerEventArgs args)
		{
			m_isPointerOver = false;
			UpdatePseudoclasses();
			args.Handled = true;
		}

		private void OnInputTargetPointerPressed(object sender, PointerPressedEventArgs args)
		{
			Focus(); // TODO Focus type should be Pointer

			m_isPointerPressed = true;
			m_shouldShowLargeSelection =
				// TODO args.Pointer.Type == PointerType.Pen ||
				args.Pointer.Type == PointerType.Touch;

			args.Pointer.Capture(m_inputTarget);

			UpdateColorFromPoint(args.GetCurrentPoint(m_inputTarget));
			UpdatePseudoclasses();
			UpdateEllipse();

			args.Handled = true;
		}

		private void OnInputTargetPointerMoved(object sender, PointerEventArgs args)
		{
			if (!m_isPointerPressed)
			{
				return;
			}

			UpdateColorFromPoint(args.GetCurrentPoint(m_inputTarget));
			args.Handled = true;
		}

		private void OnInputTargetPointerReleased(object sender, PointerReleasedEventArgs args)
		{
			m_isPointerPressed = false;
			m_shouldShowLargeSelection = false;

			args.Pointer.Capture(null);
			UpdatePseudoclasses();
			UpdateEllipse();

			args.Handled = true;
		}

		private void CreateBitmapsAndColorMap()
		{
			if (m_layoutRoot == null ||
			    m_sizingGrid == null ||
			    m_inputTarget == null ||
			    m_spectrumRectangle == null ||
			    m_spectrumEllipse == null ||
			    m_spectrumOverlayRectangle == null ||
			    m_spectrumOverlayEllipse == null)
			{
				return;
			}

			// We want ColorSpectrum to always be a square, so we'll take the smaller of the dimensions
			// and size the sizing grid to that.
			var minDimension = Math.Min(m_layoutRoot.Bounds.Width, m_layoutRoot.Bounds.Height);

			if (minDimension == 0)
			{
				return;
			}

			m_sizingGrid.Width = minDimension;
			m_sizingGrid.Height = minDimension;

			if (m_sizingGrid.Clip is RectangleGeometry clip)
			{
				clip.Rect = new Rect(0, 0, (float) minDimension, (float) minDimension);
			}

			m_inputTarget.Width = minDimension;
			m_inputTarget.Height = minDimension;
			m_spectrumRectangle.Width = minDimension;
			m_spectrumRectangle.Height = minDimension;
			m_spectrumEllipse.Width = minDimension;
			m_spectrumEllipse.Height = minDimension;
			m_spectrumOverlayRectangle.Width = minDimension;
			m_spectrumOverlayRectangle.Height = minDimension;
			m_spectrumOverlayEllipse.Width = minDimension;
			m_spectrumOverlayEllipse.Height = minDimension;

			var hsvColor = HsvColor;
			var minHue = MinHue;
			var maxHue = MaxHue;
			var minSaturation = MinSaturation;
			var maxSaturation = MaxSaturation;
			var minValue = MinValue;
			var maxValue = MaxValue;
			var shape = Shape;
			var components = Components;

			// If min >= max, then by convention, min is the only number that a property can have.
			if (minHue >= maxHue)
			{
				maxHue = minHue;
			}

			if (minSaturation >= maxSaturation)
			{
				maxSaturation = minSaturation;
			}

			if (minValue >= maxValue)
			{
				maxValue = minValue;
			}

			var hsv = (Hsv) hsvColor;

			// The middle 4 are only needed and used in the case of hue as the third dimension.
			// Saturation and luminosity need only a min and max.
			var bgraMinPixelData = new List<byte>();
			var bgraMiddle1PixelData = new List<byte>();
			var bgraMiddle2PixelData = new List<byte>();
			var bgraMiddle3PixelData = new List<byte>();
			var bgraMiddle4PixelData = new List<byte>();
			var bgraMaxPixelData = new List<byte>();
			var newHsvValues = new List<Hsv>();

			var pixelCount = (int) (Math.Round(minDimension) * Math.Round(minDimension));
			var pixelDataSize = pixelCount * 4;
			bgraMinPixelData.Capacity = pixelDataSize;

			// We'll only save pixel data for the middle bitmaps if our third dimension is hue.
			if (components == ColorSpectrumComponents.ValueSaturation ||
			    components == ColorSpectrumComponents.SaturationValue)
			{
				bgraMiddle1PixelData.Capacity = pixelDataSize;
				bgraMiddle2PixelData.Capacity = pixelDataSize;
				bgraMiddle3PixelData.Capacity = pixelDataSize;
				bgraMiddle4PixelData.Capacity = pixelDataSize;
			}

			bgraMaxPixelData.Capacity = pixelDataSize;
			newHsvValues.Capacity = pixelCount;

			var minDimensionInt = (int) Math.Round(minDimension);


			// As the user perceives it, every time the third dimension not represented in the ColorSpectrum changes,
			// the ColorSpectrum will visually change to accommodate that value.  For example, if the ColorSpectrum handles hue and luminosity,
			// and the saturation externally goes from 1.0 to 0.5, then the ColorSpectrum will visually change to look more washed out
			// to represent that third dimension's new value.
			// Internally, however, we don't want to regenerate the ColorSpectrum bitmap every single time this happens, since that's very expensive.
			// In order to make it so that we don't have to, we implement an optimization where, rather than having only one bitmap,
			// we instead have multiple that we blend together using opacity to create the effect that we want.
			// In the case where the third dimension is saturation or luminosity, we only need two: one bitmap at the minimum value
			// of the third dimension, and one bitmap at the maximum.  Then we set the second's opacity at whatever the value of
			// the third dimension is - e.G., a saturation of 0.5 implies an opacity of 50%.
			// In the case where the third dimension is hue, we need six: one bitmap corresponding to red, yellow, green, cyan, blue, and purple.
			// We'll then blend between whichever colors our hue exists between - e.G., an orange color would use red and yellow with an opacity of 50%.
			// This optimization does incur slightly more startup time initially since we have to generate multiple bitmaps at once instead of only one,
			// but the running time savings after that are *huge* when we can just set an opacity instead of generating a brand new bitmap.
			if (shape == ColorSpectrumShape.Box)
			{
				for (var x = minDimensionInt - 1; x >= 0; --x)
				{
					for (var y = minDimensionInt - 1; y >= 0; --y)
					{
						FillPixelForBox(
							x, y, hsv, minDimensionInt, components, minHue, maxHue, minSaturation, maxSaturation,
							minValue, maxValue,
							bgraMinPixelData, bgraMiddle1PixelData, bgraMiddle2PixelData, bgraMiddle3PixelData,
							bgraMiddle4PixelData, bgraMaxPixelData,
							newHsvValues);
					}
				}
			}
			else
			{
				for (var y = 0; y < minDimensionInt; ++y)
				{
					for (var x = 0; x < minDimensionInt; ++x)
					{
						FillPixelForRing(
							x, y, minDimensionInt / 2.0, hsv, components, minHue, maxHue, minSaturation, maxSaturation,
							minValue, maxValue,
							bgraMinPixelData, bgraMiddle1PixelData, bgraMiddle2PixelData, bgraMiddle3PixelData,
							bgraMiddle4PixelData, bgraMaxPixelData,
							newHsvValues);
					}
				}
			}

			var pixelWidth = (int) Math.Round(minDimension);
			var pixelHeight = (int) Math.Round(minDimension);

#pragma warning disable CA2000 // Dispose objects before losing scope
			var minBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMinPixelData);
			var maxBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMaxPixelData);
#pragma warning restore CA2000 // Dispose objects before losing scope

			switch (components)
			{
				case ColorSpectrumComponents.HueValue:
				case ColorSpectrumComponents.ValueHue:
					m_saturationMinimumBitmap = minBitmap;
					m_saturationMaximumBitmap = maxBitmap;
					break;
				case ColorSpectrumComponents.HueSaturation:
				case ColorSpectrumComponents.SaturationHue:
					m_valueBitmap = maxBitmap;
					break;
				case ColorSpectrumComponents.ValueSaturation:
				case ColorSpectrumComponents.SaturationValue:
					m_hueRedBitmap = minBitmap;
					m_hueYellowBitmap =
						ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle1PixelData);
					m_hueGreenBitmap =
						ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle2PixelData);
					m_hueCyanBitmap =
						ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle3PixelData);
					m_hueBlueBitmap =
						ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle4PixelData);
					m_huePurpleBitmap = maxBitmap;
					break;
			}

			m_shapeFromLastBitmapCreation = Shape;
			m_componentsFromLastBitmapCreation = Components;
			m_imageWidthFromLastBitmapCreation = minDimension;
			m_imageHeightFromLastBitmapCreation = minDimension;
			m_minHueFromLastBitmapCreation = MinHue;
			m_maxHueFromLastBitmapCreation = MaxHue;
			m_minSaturationFromLastBitmapCreation = MinSaturation;
			m_maxSaturationFromLastBitmapCreation = MaxSaturation;
			m_minValueFromLastBitmapCreation = MinValue;
			m_maxValueFromLastBitmapCreation = MaxValue;

			m_hsvValues = newHsvValues;

			UpdateBitmapSources();
			UpdateEllipse();
		}

		private static void FillPixelForBox(int x, int y, Hsv baseHsv, int minDimension,
			ColorSpectrumComponents components, int minHue, int maxHue, int minSaturation, int maxSaturation,
			int minValue, int maxValue, List<byte> bgraMinPixelData, List<byte> bgraMiddle1PixelData,
			List<byte> bgraMiddle2PixelData, List<byte> bgraMiddle3PixelData, List<byte> bgraMiddle4PixelData,
			List<byte> bgraMaxPixelData, List<Hsv> newHsvValues)
		{
			var hMin = minHue;
			var hMax = maxHue;
			var sMin = minSaturation / 100.0;
			var sMax = maxSaturation / 100.0;
			var vMin = minValue / 100.0;
			var vMax = maxValue / 100.0;

			var hsvMin = baseHsv;
			var hsvMiddle1 = baseHsv;
			var hsvMiddle2 = baseHsv;
			var hsvMiddle3 = baseHsv;
			var hsvMiddle4 = baseHsv;
			var hsvMax = baseHsv;

			var xPercent = (minDimension - 1.0 - x) / (minDimension - 1);
			var yPercent = (minDimension - 1.0 - y) / (minDimension - 1);

			switch (components)
			{
				case ColorSpectrumComponents.HueValue:
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H =
						hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + yPercent * (hMax - hMin);
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V =
						hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + xPercent * (vMax - vMin);
					hsvMin.S = 0;
					hsvMax.S = 1;
					break;

				case ColorSpectrumComponents.HueSaturation:
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H =
						hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + yPercent * (hMax - hMin);
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S =
						hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + xPercent * (sMax - sMin);
					hsvMin.V = 0;
					hsvMax.V = 1;
					break;

				case ColorSpectrumComponents.ValueHue:
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V =
						hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + yPercent * (vMax - vMin);
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H =
						hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + xPercent * (hMax - hMin);
					hsvMin.S = 0;
					hsvMax.S = 1;
					break;

				case ColorSpectrumComponents.ValueSaturation:
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V =
						hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + yPercent * (vMax - vMin);
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S =
						hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + xPercent * (sMax - sMin);
					hsvMin.H = 0;
					hsvMiddle1.H = 60;
					hsvMiddle2.H = 120;
					hsvMiddle3.H = 180;
					hsvMiddle4.H = 240;
					hsvMax.H = 300;
					break;

				case ColorSpectrumComponents.SaturationHue:
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S =
						hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + yPercent * (sMax - sMin);
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H =
						hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + xPercent * (hMax - hMin);
					hsvMin.V = 0;
					hsvMax.V = 1;
					break;

				case ColorSpectrumComponents.SaturationValue:
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S =
						hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + yPercent * (sMax - sMin);
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V =
						hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + xPercent * (vMax - vMin);
					hsvMin.H = 0;
					hsvMiddle1.H = 60;
					hsvMiddle2.H = 120;
					hsvMiddle3.H = 180;
					hsvMiddle4.H = 240;
					hsvMax.H = 300;
					break;
			}

			// If saturation is an axis in the spectrum with hue, or value is an axis, then we want
			// that axis to go from maximum at the top to minimum at the bottom,
			// or maximum at the outside to minimum at the inside in the case of the ring configuration,
			// so we'll invert the number before assigning the HSL value to the array.
			// Otherwise, we'll have a very narrow section in the middle that actually has meaningful hue
			// in the case of the ring configuration.
			if (components == ColorSpectrumComponents.HueSaturation ||
			    components == ColorSpectrumComponents.SaturationHue)
			{
				hsvMin.S = sMax - hsvMin.S + sMin;
				hsvMiddle1.S = sMax - hsvMiddle1.S + sMin;
				hsvMiddle2.S = sMax - hsvMiddle2.S + sMin;
				hsvMiddle3.S = sMax - hsvMiddle3.S + sMin;
				hsvMiddle4.S = sMax - hsvMiddle4.S + sMin;
				hsvMax.S = sMax - hsvMax.S + sMin;
			}
			else
			{
				hsvMin.V = vMax - hsvMin.V + vMin;
				hsvMiddle1.V = vMax - hsvMiddle1.V + vMin;
				hsvMiddle2.V = vMax - hsvMiddle2.V + vMin;
				hsvMiddle3.V = vMax - hsvMiddle3.V + vMin;
				hsvMiddle4.V = vMax - hsvMiddle4.V + vMin;
				hsvMax.V = vMax - hsvMax.V + vMin;
			}

			newHsvValues.Add(hsvMin);

			var rgbMin = ColorConversion.HsvToRgb(hsvMin);
			bgraMinPixelData.Add((byte) Math.Round(rgbMin.B * 255)); // b
			bgraMinPixelData.Add((byte) Math.Round(rgbMin.G * 255)); // g
			bgraMinPixelData.Add((byte) Math.Round(rgbMin.R * 255)); // r
			bgraMinPixelData.Add(255); // a - ignored

			// We'll only save pixel data for the middle bitmaps if our third dimension is hue.
			if (components == ColorSpectrumComponents.ValueSaturation ||
			    components == ColorSpectrumComponents.SaturationValue)
			{
				var rgbMiddle1 = ColorConversion.HsvToRgb(hsvMiddle1);
				bgraMiddle1PixelData.Add((byte) Math.Round(rgbMiddle1.B * 255)); // b
				bgraMiddle1PixelData.Add((byte) Math.Round(rgbMiddle1.G * 255)); // g
				bgraMiddle1PixelData.Add((byte) Math.Round(rgbMiddle1.R * 255)); // r
				bgraMiddle1PixelData.Add(255); // a - ignored

				var rgbMiddle2 = ColorConversion.HsvToRgb(hsvMiddle2);
				bgraMiddle2PixelData.Add((byte) Math.Round(rgbMiddle2.B * 255)); // b
				bgraMiddle2PixelData.Add((byte) Math.Round(rgbMiddle2.G * 255)); // g
				bgraMiddle2PixelData.Add((byte) Math.Round(rgbMiddle2.R * 255)); // r
				bgraMiddle2PixelData.Add(255); // a - ignored

				var rgbMiddle3 = ColorConversion.HsvToRgb(hsvMiddle3);
				bgraMiddle3PixelData.Add((byte) Math.Round(rgbMiddle3.B * 255)); // b
				bgraMiddle3PixelData.Add((byte) Math.Round(rgbMiddle3.G * 255)); // g
				bgraMiddle3PixelData.Add((byte) Math.Round(rgbMiddle3.R * 255)); // r
				bgraMiddle3PixelData.Add(255); // a - ignored

				var rgbMiddle4 = ColorConversion.HsvToRgb(hsvMiddle4);
				bgraMiddle4PixelData.Add((byte) Math.Round(rgbMiddle4.B * 255)); // b
				bgraMiddle4PixelData.Add((byte) Math.Round(rgbMiddle4.G * 255)); // g
				bgraMiddle4PixelData.Add((byte) Math.Round(rgbMiddle4.R * 255)); // r
				bgraMiddle4PixelData.Add(255); // a - ignored
			}

			var rgbMax = ColorConversion.HsvToRgb(hsvMax);
			bgraMaxPixelData.Add((byte) Math.Round(rgbMax.B * 255)); // b
			bgraMaxPixelData.Add((byte) Math.Round(rgbMax.G * 255)); // g
			bgraMaxPixelData.Add((byte) Math.Round(rgbMax.R * 255)); // r
			bgraMaxPixelData.Add(255); // a - ignored
		}

		private static void FillPixelForRing(int x, int y, double radius, Hsv baseHsv,
			ColorSpectrumComponents components, int minHue, int maxHue, int minSaturation, int maxSaturation,
			int minValue, int maxValue, List<byte> bgraMinPixelData, List<byte> bgraMiddle1PixelData,
			List<byte> bgraMiddle2PixelData, List<byte> bgraMiddle3PixelData, List<byte> bgraMiddle4PixelData,
			List<byte> bgraMaxPixelData, List<Hsv> newHsvValues)
		{
			var hMin = minHue;
			var hMax = maxHue;
			var sMin = minSaturation / 100.0;
			var sMax = maxSaturation / 100.0;
			var vMin = minValue / 100.0;
			var vMax = maxValue / 100.0;

			var distanceFromRadius = Math.Sqrt(Math.Pow(x - radius, 2) + Math.Pow(y - radius, 2));

			double xToUse = x;
			double yToUse = y;

			// If we're outside the ring, then we want the pixel to appear as blank.
			// However, to avoid issues with rounding errors, we'll act as though this point
			// is on the edge of the ring for the purposes of returning an HSL value.
			// That way, hittesting on the edges will always return the correct value.
			if (distanceFromRadius > radius)
			{
				xToUse = radius / distanceFromRadius * (x - radius) + radius;
				yToUse = radius / distanceFromRadius * (y - radius) + radius;
				distanceFromRadius = radius;
			}

			var hsvMin = baseHsv;
			var hsvMiddle1 = baseHsv;
			var hsvMiddle2 = baseHsv;
			var hsvMiddle3 = baseHsv;
			var hsvMiddle4 = baseHsv;
			var hsvMax = baseHsv;

			var r = 1 - distanceFromRadius / radius;

			var theta = Math.Atan2(radius - yToUse, radius - xToUse) * 180.0 / Math.PI;
			theta += 180.0;
			theta = Math.Floor(theta);

			while (theta > 360)
			{
				theta -= 360;
			}

			var thetaPercent = theta / 360;

			switch (components)
			{
				case ColorSpectrumComponents.HueValue:
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H =
						hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + thetaPercent * (hMax - hMin);
					hsvMin.V = hsvMiddle1.V =
						hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + r * (vMax - vMin);
					hsvMin.S = 0;
					hsvMax.S = 1;
					break;

				case ColorSpectrumComponents.HueSaturation:
					hsvMin.H = hsvMiddle1.H = hsvMiddle2.H =
						hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + thetaPercent * (hMax - hMin);
					hsvMin.S = hsvMiddle1.S =
						hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + r * (sMax - sMin);
					hsvMin.V = 0;
					hsvMax.V = 1;
					break;

				case ColorSpectrumComponents.ValueHue:
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V =
						hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + thetaPercent * (vMax - vMin);
					hsvMin.H = hsvMiddle1.H =
						hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + r * (hMax - hMin);
					hsvMin.S = 0;
					hsvMax.S = 1;
					break;

				case ColorSpectrumComponents.ValueSaturation:
					hsvMin.V = hsvMiddle1.V = hsvMiddle2.V =
						hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + thetaPercent * (vMax - vMin);
					hsvMin.S = hsvMiddle1.S =
						hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + r * (sMax - sMin);
					hsvMin.H = 0;
					hsvMiddle1.H = 60;
					hsvMiddle2.H = 120;
					hsvMiddle3.H = 180;
					hsvMiddle4.H = 240;
					hsvMax.H = 300;
					break;

				case ColorSpectrumComponents.SaturationHue:
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S =
						hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + thetaPercent * (sMax - sMin);
					hsvMin.H = hsvMiddle1.H =
						hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + r * (hMax - hMin);
					hsvMin.V = 0;
					hsvMax.V = 1;
					break;

				case ColorSpectrumComponents.SaturationValue:
					hsvMin.S = hsvMiddle1.S = hsvMiddle2.S =
						hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + thetaPercent * (sMax - sMin);
					hsvMin.V = hsvMiddle1.V =
						hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + r * (vMax - vMin);
					hsvMin.H = 0;
					hsvMiddle1.H = 60;
					hsvMiddle2.H = 120;
					hsvMiddle3.H = 180;
					hsvMiddle4.H = 240;
					hsvMax.H = 300;
					break;
			}

			// If saturation is an axis in the spectrum with hue, or value is an axis, then we want
			// that axis to go from maximum at the top to minimum at the bottom,
			// or maximum at the outside to minimum at the inside in the case of the ring configuration,
			// so we'll invert the number before assigning the HSL value to the array.
			// Otherwise, we'll have a very narrow section in the middle that actually has meaningful hue
			// in the case of the ring configuration.
			if (components == ColorSpectrumComponents.HueSaturation ||
			    components == ColorSpectrumComponents.SaturationHue)
			{
				hsvMin.S = sMax - hsvMin.S + sMin;
				hsvMiddle1.S = sMax - hsvMiddle1.S + sMin;
				hsvMiddle2.S = sMax - hsvMiddle2.S + sMin;
				hsvMiddle3.S = sMax - hsvMiddle3.S + sMin;
				hsvMiddle4.S = sMax - hsvMiddle4.S + sMin;
				hsvMax.S = sMax - hsvMax.S + sMin;
			}
			else
			{
				hsvMin.V = vMax - hsvMin.V + vMin;
				hsvMiddle1.V = vMax - hsvMiddle1.V + vMin;
				hsvMiddle2.V = vMax - hsvMiddle2.V + vMin;
				hsvMiddle3.V = vMax - hsvMiddle3.V + vMin;
				hsvMiddle4.V = vMax - hsvMiddle4.V + vMin;
				hsvMax.V = vMax - hsvMax.V + vMin;
			}

			newHsvValues.Add(hsvMin);

			var rgbMin = ColorConversion.HsvToRgb(hsvMin);
			bgraMinPixelData.Add((byte) Math.Round(rgbMin.B * 255)); // b
			bgraMinPixelData.Add((byte) Math.Round(rgbMin.G * 255)); // g
			bgraMinPixelData.Add((byte) Math.Round(rgbMin.R * 255)); // r
			bgraMinPixelData.Add(255); // a

			// We'll only save pixel data for the middle bitmaps if our third dimension is hue.
			if (components == ColorSpectrumComponents.ValueSaturation ||
			    components == ColorSpectrumComponents.SaturationValue)
			{
				var rgbMiddle1 = ColorConversion.HsvToRgb(hsvMiddle1);
				bgraMiddle1PixelData.Add((byte) Math.Round(rgbMiddle1.B * 255)); // b
				bgraMiddle1PixelData.Add((byte) Math.Round(rgbMiddle1.G * 255)); // g
				bgraMiddle1PixelData.Add((byte) Math.Round(rgbMiddle1.R * 255)); // r
				bgraMiddle1PixelData.Add(255); // a

				var rgbMiddle2 = ColorConversion.HsvToRgb(hsvMiddle2);
				bgraMiddle2PixelData.Add((byte) Math.Round(rgbMiddle2.B * 255)); // b
				bgraMiddle2PixelData.Add((byte) Math.Round(rgbMiddle2.G * 255)); // g
				bgraMiddle2PixelData.Add((byte) Math.Round(rgbMiddle2.R * 255)); // r
				bgraMiddle2PixelData.Add(255); // a

				var rgbMiddle3 = ColorConversion.HsvToRgb(hsvMiddle3);
				bgraMiddle3PixelData.Add((byte) Math.Round(rgbMiddle3.B * 255)); // b
				bgraMiddle3PixelData.Add((byte) Math.Round(rgbMiddle3.G * 255)); // g
				bgraMiddle3PixelData.Add((byte) Math.Round(rgbMiddle3.R * 255)); // r
				bgraMiddle3PixelData.Add(255); // a

				var rgbMiddle4 = ColorConversion.HsvToRgb(hsvMiddle4);
				bgraMiddle4PixelData.Add((byte) Math.Round(rgbMiddle4.B * 255)); // b
				bgraMiddle4PixelData.Add((byte) Math.Round(rgbMiddle4.G * 255)); // g
				bgraMiddle4PixelData.Add((byte) Math.Round(rgbMiddle4.R * 255)); // r
				bgraMiddle4PixelData.Add(255); // a
			}

			var rgbMax = ColorConversion.HsvToRgb(hsvMax);
			bgraMaxPixelData.Add((byte) Math.Round(rgbMax.B * 255)); // b
			bgraMaxPixelData.Add((byte) Math.Round(rgbMax.G * 255)); // g
			bgraMaxPixelData.Add((byte) Math.Round(rgbMax.R * 255)); // r
			bgraMaxPixelData.Add(255); // a
		}

		private void UpdateBitmapSources()
		{
			if (m_spectrumOverlayRectangle == null ||
			    m_spectrumOverlayEllipse == null ||
			    m_spectrumRectangle == null ||
			    m_spectrumEllipse == null)
			{
				return;
			}

			var hsvColor = HsvColor;
			var components = Components;


			// We'll set the base image and the overlay image based on which component is our third dimension.
			// If it's saturation or luminosity, then the base image is that dimension at its minimum value,
			// while the overlay image is that dimension at its maximum value.
			// If it's hue, then we'll figure out where in the color wheel we are, and then use the two
			// colors on either side of our position as our base image and overlay image.
			// For example, if our hue is orange, then the base image would be red and the overlay image yellow.
			switch (components)
			{
				case ColorSpectrumComponents.HueValue:
				case ColorSpectrumComponents.ValueHue:
				{
					if (m_saturationMinimumBitmap == null ||
					    m_saturationMaximumBitmap == null)
					{
						return;
					}

					var spectrumBrush = new ImageBrush(m_saturationMinimumBitmap);
					var spectrumOverlayBrush = new ImageBrush(m_saturationMaximumBitmap);

					m_spectrumOverlayRectangle.Opacity = ((Hsv) hsvColor).S;
					m_spectrumOverlayEllipse.Opacity = ((Hsv) hsvColor).S;
					m_spectrumRectangle.Fill = spectrumBrush;
					m_spectrumEllipse.Fill = spectrumBrush;
					m_spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
					m_spectrumOverlayEllipse.Fill = spectrumOverlayBrush;
					break;
				}
				case ColorSpectrumComponents.HueSaturation:
				case ColorSpectrumComponents.SaturationHue:
				{
					if (m_valueBitmap == null)
					{
						return;
					}

					var spectrumBrush = new ImageBrush(m_valueBitmap);
					var spectrumOverlayBrush = new ImageBrush(m_valueBitmap);

					m_spectrumOverlayRectangle.Opacity = 1;
					m_spectrumOverlayEllipse.Opacity = 1;
					m_spectrumRectangle.Fill = spectrumBrush;
					m_spectrumEllipse.Fill = spectrumBrush;
					m_spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
					m_spectrumOverlayEllipse.Fill = spectrumOverlayBrush;
					break;
				}
				case ColorSpectrumComponents.ValueSaturation:
				case ColorSpectrumComponents.SaturationValue:
				{
					if (m_hueRedBitmap == null ||
					    m_hueYellowBitmap == null ||
					    m_hueGreenBitmap == null ||
					    m_hueCyanBitmap == null ||
					    m_hueBlueBitmap == null ||
					    m_huePurpleBitmap == null)
					{
						return;
					}

					ImageBrush spectrumBrush;
					ImageBrush spectrumOverlayBrush;

					var sextant = ((Hsv) hsvColor).H / 60.0;

					if (sextant < 1)
					{
						spectrumBrush = new ImageBrush(m_hueRedBitmap);
						spectrumOverlayBrush = new ImageBrush(m_hueYellowBitmap);
					}
					else if (sextant >= 1 && sextant < 2)
					{
						spectrumBrush = new ImageBrush(m_hueYellowBitmap);
						spectrumOverlayBrush = new ImageBrush(m_hueGreenBitmap);
					}
					else if (sextant >= 2 && sextant < 3)
					{
						spectrumBrush = new ImageBrush(m_hueGreenBitmap);
						spectrumOverlayBrush = new ImageBrush(m_hueCyanBitmap);
					}
					else if (sextant >= 3 && sextant < 4)
					{
						spectrumBrush = new ImageBrush(m_hueCyanBitmap);
						spectrumOverlayBrush = new ImageBrush(m_hueBlueBitmap);
					}
					else if (sextant >= 4 && sextant < 5)
					{
						spectrumBrush = new ImageBrush(m_hueBlueBitmap);
						spectrumOverlayBrush = new ImageBrush(m_huePurpleBitmap);
					}
					else
					{
						spectrumBrush = new ImageBrush(m_huePurpleBitmap);
						spectrumOverlayBrush = new ImageBrush(m_hueRedBitmap);
					}

					m_spectrumOverlayRectangle.Opacity = sextant - (int) sextant;
					m_spectrumOverlayEllipse.Opacity = sextant - (int) sextant;
					m_spectrumRectangle.Fill = spectrumBrush;
					m_spectrumEllipse.Fill = spectrumBrush;
					m_spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
					m_spectrumOverlayEllipse.Fill = spectrumOverlayBrush;
					break;
				}
			}
		}

		private bool SelectionEllipseShouldBeLight()
		{
			// The selection ellipse should be light if and only if the chosen color
			// contrasts more with black than it does with white.
			// To find how much something contrasts with white, we use the equation
			// for relative luminance, which is given by
			//
			// L = 0.2126 * Rg + 0.7152 * Gg + 0.0722 * Bg
			//
			// where Xg = { X/3294 if X <= 10, (R/269 + 0.0513)^2.4 otherwise }
			//
			// If L is closer to 1, then the color is closer to white; if it is closer to 0,
			// then the color is closer to black.  This is based on the fact that the human
			// eye perceives green to be much brighter than red, which in turn is perceived to be
			// brighter than blue.
			//
			// If the third dimension is value, then we won't be updating the spectrum's displayed colors,
			// so in that case we should use a value of 1 when considering the backdrop
			// for the selection ellipse.
			Color displayedColor;

			var components = Components;
			if (components == ColorSpectrumComponents.HueSaturation ||
			    components == ColorSpectrumComponents.SaturationHue)
			{
				var hsvColorVector = HsvColor;
				var hsvColor = (Hsv) hsvColorVector;
				hsvColor.V = 1;
				var color = ColorConversion.HsvToRgb(hsvColor);
				displayedColor = ColorConversion.ColorFromRgba(color, hsvColorVector.Z);
			}
			else
			{
				displayedColor = new Color();
			}

			var rg = displayedColor.R <= 10
				? displayedColor.R / 3294.0
				: Math.Pow(displayedColor.R / 269.0 + 0.0513, 2.4);
			var gg = displayedColor.G <= 10
				? displayedColor.G / 3294.0
				: Math.Pow(displayedColor.G / 269.0 + 0.0513, 2.4);
			var bg = displayedColor.B <= 10
				? displayedColor.B / 3294.0
				: Math.Pow(displayedColor.B / 269.0 + 0.0513, 2.4);

			return 0.2126 * rg + 0.7152 * gg + 0.0722 * bg <= 0.5;
		}
	}
}