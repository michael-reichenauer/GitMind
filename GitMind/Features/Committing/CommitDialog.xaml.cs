using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.Features.Committing
{
	/// <summary>
	/// Interaction logic for CommitDialog.xaml
	/// </summary>
	public partial class CommitDialog : Window
	{
		public CommitDialog(
			Window owner, 
			string branchName, 
			string workingFolder,
			Func<string, IEnumerable<CommitFile>, Task<bool>> commitAction,
			IEnumerable<CommitFile> files,
			Command showUncommittedDiffCommand)
		{
			Owner = owner;
			InitializeComponent();
			DataContext = new CommitDialogViewModel(
				branchName, workingFolder,  commitAction, files, showUncommittedDiffCommand);
			MessageText.Focus();
		}
	}
}
