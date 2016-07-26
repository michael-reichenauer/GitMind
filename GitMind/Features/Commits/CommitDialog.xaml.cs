using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;


namespace GitMind.Features.Commits
{
	/// <summary>
	/// Interaction logic for CommitDialog.xaml
	/// </summary>
	public partial class CommitDialog : Window
	{
		public CommitDialog(
			string branchName, 
			Func<string, IReadOnlyList<string>, Task<bool>> commitAction, 
			IReadOnlyList<string> files)
		{
			InitializeComponent();
			DataContext = new CommitDialogViewModel(branchName, commitAction, files);
			MessageText.Focus();
		}
	}
}
