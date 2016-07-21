using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Log = GitMind.Utils.Log;


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
			set
			{
				OptionAuto.IsChecked = value;
				if (!value)
				{
					PromptTextBox.Focus();
				}
			}
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


		private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
		{
			try
			{
				Process proc = new Process();
				proc.StartInfo.FileName = "https://github.com/michael-reichenauer/GitMind/wiki/Help";
				proc.Start();
			}
			catch (Exception ex) when (ex.IsNotFatal())
			{
				Log.Error($"Failed to open help link {ex}");
			}
		}
	}
}
