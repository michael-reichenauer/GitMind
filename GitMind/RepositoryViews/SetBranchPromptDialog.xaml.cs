using System;
using System.Windows;


namespace GitMind.RepositoryViews
{
	/// <summary>
	/// Interaction logic for SetBranchPromptDialog.xaml
	/// </summary>
	public partial class SetBranchPromptDialog : Window
	{
		public SetBranchPromptDialog()
		{
			InitializeComponent();
			Owner = Application.Current.MainWindow;
		}

		public string PromptText
		{
			get { return PromptTextBox.Text; }
			set
			{
				PromptTextBox.Text = value;
				PromptTextBox.Focus();
			}
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
