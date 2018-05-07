using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using GitMind.MainWindowViews;
using GitMind.Utils.Git;
using Log = GitMind.Utils.Log;


namespace GitMind.Features.Commits.Private
{
	/// <summary>
	/// Interaction logic for SetBranchPromptDialog.xaml
	/// </summary>
	public partial class SetBranchPromptDialog : Window
	{
		internal SetBranchPromptDialog(WindowOwner owner)
		{
			InitializeComponent();
			Owner = owner;
		}


		public bool IsAutomatically
		{
			get { return OptionAuto.IsChecked ?? false; }
			set
			{
				OptionAuto.IsChecked = value;
				OptionManual.IsChecked = !value;
			}
		}

		public string PromptText
		{
			get { return BranchName.Text; }
			set { BranchName.Text = value; }
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
				proc.StartInfo.FileName = "https://github.com/michael-reichenauer/GitMind/wiki/Help#set-branch";
				proc.Start();
			}
			catch (Exception ex) when (ex.IsNotFatal())
			{
				Log.Exception(ex, "Failed to open help link");
			}
		}


		private void BranchName_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			OptionManual.IsChecked = true;
		}


		public void AddBranchName(BranchName name)
		{
			BranchName.Items.Add(name);
		}
	}
}
