using System.Windows;

namespace GitMind.CommitsHistory
{
	/// <summary>
	/// Interaction logic for SetBranchPrompt.xaml
	/// </summary>
	public partial class SetBranchPrompt : Window
	{
		public SetBranchPrompt()
		{
			InitializeComponent();
			Owner = Application.Current.MainWindow;
		}

		public string PromptText
		{
			get { return PromptTextBox.Text; }
			set { PromptTextBox.Text = value; }
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}
	}
}
