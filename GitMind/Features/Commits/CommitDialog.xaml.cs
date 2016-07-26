using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Utils.UI;


namespace GitMind.Features.Commits
{
	/// <summary>
	/// Interaction logic for CommitDialog.xaml
	/// </summary>
	public partial class CommitDialog : Window
	{
		public CommitDialog(
			Window owner, 
			string branchName, 
			Func<string, IReadOnlyList<string>, Task<bool>> commitAction, 
			IReadOnlyList<string> files,
			Command showUncommittedDiffCommand)
		{
			Owner = owner;
			InitializeComponent();
			DataContext = new CommitDialogViewModel(branchName, commitAction, files, showUncommittedDiffCommand);
			MessageText.Focus();
		}
	}
}
