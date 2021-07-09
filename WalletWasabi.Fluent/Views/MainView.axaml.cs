using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaWinUI.ColorPicker;

namespace WalletWasabi.Fluent.Views
{
	public class MainView : UserControl
	{
		public MainView()
		{
			InitializeComponent();
			var ColorPicker = this.FindControl<ColorPicker>("ColorPicker");

			ColorPicker.Color = Color.Parse("#89134074");
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}