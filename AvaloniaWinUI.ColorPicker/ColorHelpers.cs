using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvaloniaWinUI.ColorPicker
{
	internal enum IncrementDirection
	{
		Lower,
		Higher
	}

	internal enum IncrementAmount
	{
		Small,
		Large
	}

	internal static class ColorHelpers
	{
		internal static string ToDisplayName(Color color)
		{
			return color.ToString();
		}

		public static Hsv IncrementColorChannel(
			Hsv originalHsv,
			ColorPickerHsvChannel channel,
			IncrementDirection direction,
			IncrementAmount amount,
			bool shouldWrap,
			double minBound,
			double maxBound)
		{
			var newHsv = originalHsv;

			if (amount == IncrementAmount.Small)
			{
				// In order to avoid working with small values that can incur rounding issues,
				// we'll multiple saturation and value by 100 to put them in the range of 0-100 instead of 0-1.
				newHsv.S *= 100;
				newHsv.V *= 100;

				ref var valueToIncrement = ref newHsv.H;
				double incrementAmount = 0;

				// If we're adding a small increment, then we'll just add or subtract 1.
				// If we're adding a large increment, then we want to snap to the next
				// or previous major value - for hue, this is every increment of 30;
				// for saturation and value, this is every increment of 10.
				switch (channel)
				{
					case ColorPickerHsvChannel.Hue:
						valueToIncrement = ref newHsv.H;
						incrementAmount = amount == IncrementAmount.Small ? 1 : 30;
						break;

					case ColorPickerHsvChannel.Saturation:
						valueToIncrement = ref newHsv.S;
						incrementAmount = amount == IncrementAmount.Small ? 1 : 10;
						break;

					case ColorPickerHsvChannel.Value:
						valueToIncrement = ref newHsv.V;
						incrementAmount = amount == IncrementAmount.Small ? 1 : 10;
						break;

					default:
						throw new InvalidOperationException("Invalid ColorPickerHsvChannel.");
				}

				var previousValue = valueToIncrement;

				valueToIncrement += direction == IncrementDirection.Lower ? -incrementAmount : incrementAmount;

				// If the value has reached outside the bounds, we were previous at the boundary, and we should wrap,
				// then we'll place the selection on the other side of the spectrum.
				// Otherwise, we'll place it on the boundary that was exceeded.
				if (valueToIncrement < minBound)
				{
					valueToIncrement = shouldWrap && previousValue == minBound ? maxBound : minBound;
				}

				if (valueToIncrement > maxBound)
				{
					valueToIncrement = shouldWrap && previousValue == maxBound ? minBound : maxBound;
				}

				// We multiplied saturation and value by 100 previously, so now we want to put them back in the 0-1 range.
				newHsv.S /= 100;
				newHsv.V /= 100;
			}
			else
			{
				// While working with named colors, we're going to need to be working in actual HSV units,
				// so we'll divide the min bound and max bound by 100 in the case of saturation or value,
				// since we'll have received units between 0-100 and we need them within 0-1.
				if (channel == ColorPickerHsvChannel.Saturation ||
				    channel == ColorPickerHsvChannel.Value)
				{
					minBound /= 100;
					maxBound /= 100;
				}

				newHsv = FindNextNamedColor(originalHsv, channel, direction, shouldWrap, minBound, maxBound);
			}

			return newHsv;
		}

		private static Hsv FindNextNamedColor(
			Hsv originalHsv,
			ColorPickerHsvChannel channel,
			IncrementDirection direction,
			bool shouldWrap,
			double minBound,
			double maxBound)
		{
			// There's no easy way to directly get the next named color, so what we'll do
			// is just iterate in the direction that we want to find it until we find a color
			// in that direction that has a color name different than our current color name.
			// Once we find a new color name, then we'll iterate across that color name until
			// we find its bounds on the other side, and then select the color that is exactly
			// in the middle of that color's bounds.
			var newHsv = originalHsv;

			var originalColorName = ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(originalHsv)));
			var newColorName = originalColorName;

			double originalValue = 0;
			double empty = 0;
			ref var newValue = ref empty;
			double incrementAmount = 0;

			switch (channel)
			{
				case ColorPickerHsvChannel.Hue:
					originalValue = originalHsv.H;
					newValue = ref newHsv.H;
					incrementAmount = 1;
					break;

				case ColorPickerHsvChannel.Saturation:
					originalValue = originalHsv.S;
					newValue = ref newHsv.S;
					incrementAmount = 0.01;
					break;

				case ColorPickerHsvChannel.Value:
					originalValue = originalHsv.V;
					newValue = ref newHsv.V;
					incrementAmount = 0.01;
					break;

				default:
					throw new InvalidOperationException("Invalid ColorPickerHsvChannel.");
			}

			var shouldFindMidPoint = true;

			while (newColorName == originalColorName)
			{
				var previousValue = newValue;
				newValue += (direction == IncrementDirection.Lower ? -1 : 1) * incrementAmount;

				var justWrapped = false;

				// If we've hit a boundary, then either we should wrap or we shouldn't.
				// If we should, then we'll perform that wrapping if we were previously up against
				// the boundary that we've now hit.  Otherwise, we'll stop at that boundary.
				if (newValue > maxBound)
				{
					if (shouldWrap)
					{
						newValue = minBound;
						justWrapped = true;
					}
					else
					{
						newValue = maxBound;
						shouldFindMidPoint = false;
						newColorName = ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(newHsv)));
						break;
					}
				}
				else if (newValue < minBound)
				{
					if (shouldWrap)
					{
						newValue = maxBound;
						justWrapped = true;
					}
					else
					{
						newValue = minBound;
						shouldFindMidPoint = false;
						newColorName = ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(newHsv)));
						break;
					}
				}

				if (!justWrapped &&
				    previousValue != originalValue &&
				    Math.Sign(newValue - originalValue) != Math.Sign(previousValue - originalValue))
				{
					// If we've wrapped all the way back to the start and have failed to find a new color name,
					// then we'll just quit - there isn't a new color name that we're going to find.
					shouldFindMidPoint = false;
					break;
				}

				newColorName = ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(newHsv)));
			}

			if (shouldFindMidPoint)
			{
				var startHsv = newHsv;
				var currentHsv = startHsv;
				double startEndOffset = 0;
				var currentColorName = newColorName;

				double empty1 = 0, empty2 = 0;
				ref var startValue = ref empty1;
				ref var currentValue = ref empty2;
				double wrapIncrement = 0;

				switch (channel)
				{
					case ColorPickerHsvChannel.Hue:
						startValue = ref startHsv.H;
						currentValue = ref currentHsv.H;
						wrapIncrement = 360.0;
						break;

					case ColorPickerHsvChannel.Saturation:
						startValue = ref startHsv.S;
						currentValue = ref currentHsv.S;
						wrapIncrement = 1.0;
						break;

					case ColorPickerHsvChannel.Value:
						startValue = ref startHsv.V;
						currentValue = ref currentHsv.V;
						wrapIncrement = 1.0;
						break;

					default:
						throw new InvalidOperationException("Invalid ColorPickerHsvChannel.");
				}

				while (newColorName == currentColorName)
				{
					currentValue += (direction == IncrementDirection.Lower ? -1 : 1) * incrementAmount;

					// If we've hit a boundary, then either we should wrap or we shouldn't.
					// If we should, then we'll perform that wrapping if we were previously up against
					// the boundary that we've now hit.  Otherwise, we'll stop at that boundary.
					if (currentValue > maxBound)
					{
						if (shouldWrap)
						{
							currentValue = minBound;
							startEndOffset = maxBound - minBound;
						}
						else
						{
							currentValue = maxBound;
							break;
						}
					}
					else if (currentValue < minBound)
					{
						if (shouldWrap)
						{
							currentValue = maxBound;
							startEndOffset = minBound - maxBound;
						}
						else
						{
							currentValue = minBound;
							break;
						}
					}

					currentColorName =
						ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(currentHsv)));
				}

				newValue = (startValue + currentValue + startEndOffset) / 2;

				// Dividing by 2 may have gotten us halfway through a single step, so we'll
				// remove that half-step if it exists.
				var leftoverValue = Math.Abs(newValue);

				while (leftoverValue > incrementAmount)
				{
					leftoverValue -= incrementAmount;
				}

				newValue -= leftoverValue;

				while (newValue < minBound)
				{
					newValue += wrapIncrement;
				}

				while (newValue > maxBound)
				{
					newValue -= wrapIncrement;
				}
			}

			return newHsv;
		}

		public static double IncrementAlphaChannel(
			double originalAlpha,
			IncrementDirection direction,
			IncrementAmount amount,
			bool shouldWrap,
			double minBound,
			double maxBound)
		{
			// In order to avoid working with small values that can incur rounding issues,
			// we'll multiple alpha by 100 to put it in the range of 0-100 instead of 0-1.
			originalAlpha *= 100;

			const double smallIncrementAmount = 1;
			const double largeIncrementAmount = 10;

			if (amount == IncrementAmount.Small)
			{
				originalAlpha += (direction == IncrementDirection.Lower ? -1 : 1) * smallIncrementAmount;
			}
			else
			{
				if (direction == IncrementDirection.Lower)
				{
					originalAlpha = Math.Ceiling((originalAlpha - largeIncrementAmount) / largeIncrementAmount) *
					                largeIncrementAmount;
				}
				else
				{
					originalAlpha = Math.Floor((originalAlpha + largeIncrementAmount) / largeIncrementAmount) *
					                largeIncrementAmount;
				}
			}

			// If the value has reached outside the bounds and we should wrap, then we'll place the selection
			// on the other side of the spectrum.  Otherwise, we'll place it on the boundary that was exceeded.
			if (originalAlpha < minBound)
			{
				originalAlpha = shouldWrap ? maxBound : minBound;
			}

			if (originalAlpha > maxBound)
			{
				originalAlpha = shouldWrap ? minBound : maxBound;
			}

			// We multiplied alpha by 100 previously, so now we want to put it back in the 0-1 range.
			return originalAlpha / 100;
		}

		internal static IBitmap CreateBitmapFromPixelData(int pixelWidth, int pixelHeight, List<byte> bgraMinPixelData)
		{
			var bitmap = new WriteableBitmap(new PixelSize(pixelWidth, pixelHeight), new Vector(96, 96),
				PixelFormat.Bgra8888, AlphaFormat.Premul);

			using (var fb = bitmap.Lock())
			{
				Marshal.Copy(bgraMinPixelData.ToArray(), 0, fb.Address, bgraMinPixelData.Count);
			}

			return bitmap;
		}
	}
}