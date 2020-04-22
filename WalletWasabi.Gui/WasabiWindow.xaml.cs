using Avalonia;
using Avalonia.Markup.Xaml;
using AvalonStudio.Shell.Controls;

namespace WalletWasabi.Gui
{
	public class WasabiWindow : MetroWindow
	{
		public WasabiWindow()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
