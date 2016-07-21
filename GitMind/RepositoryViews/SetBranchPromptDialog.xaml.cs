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


		public bool IsAutomatically
		{
			get { return OptionAuto.IsChecked ?? false; }
			set { OptionAuto.IsChecked = value; }
		}

		public string PromptText
		{
			get { return PromptTextBox.Text; }
			set
			{
				IsAutomatically = string.IsNullOrEmpty(value);
				OptionManual.IsChecked = !string.IsNullOrEmpty(value);
				PromptTextBox.Text = value;
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
