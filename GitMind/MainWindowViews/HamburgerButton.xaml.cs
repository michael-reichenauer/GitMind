using System.Windows;
using System.Windows.Controls;


namespace GitMind.MainWindowViews
{
	/// <summary>
	/// Interaction logic for HamburgerButton.xaml
	/// </summary>
	public partial class HamburgerButton : UserControl
	{
		public HamburgerButton()
		{
			InitializeComponent();
		}


		private void HamburgerButton_OnClick(object sender, RoutedEventArgs e)
		{
			HamburgerContextMenu.PlacementTarget = this;
			HamburgerContextMenu.IsOpen = true;
		}
	}
}
