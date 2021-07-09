using System;
using Avalonia.Media;

namespace AvaloniaWinUI.ColorPicker
{
	public class ColorChangedEventArgs : EventArgs
	{
		public ColorChangedEventArgs(Color oldColor, Color newColor)
		{
			OldColor = oldColor;
			NewColor = newColor;
		}

		public Color OldColor { get; }

		public Color NewColor { get; }
	}
}