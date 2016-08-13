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
			string commitMessage,
			Command showUncommittedDiffCommand,
			Command<string> undoUncommittedFileCommand)
		{
			Owner = owner;
			InitializeComponent();
			CommitDialogViewModel viewModel = new CommitDialogViewModel(
				branchName,
				workingFolder,
				commitAction,
				files,
				commitMessage,
				showUncommittedDiffCommand,
				undoUncommittedFileCommand);

			DataContext = viewModel;

			if (string.IsNullOrWhiteSpace(viewModel.Subject))
			{
				SubjectText.Focus();
			}
			else
			{
				DescriptionText.Focus();
			}			
		}
	}
}
