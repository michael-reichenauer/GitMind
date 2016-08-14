using System;
using System.Collections.Generic;
using System.Linq;
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
		private readonly CommitDialogViewModel viewModel;

		public CommitDialog(
			Window owner,
			string branchName,
			string workingFolder,
			IEnumerable<CommitFile> files,
			string commitMessage,
			Command showUncommittedDiffCommand,
			Command<string> undoUncommittedFileCommand)
		{
			Owner = owner;
			InitializeComponent();
			viewModel = new CommitDialogViewModel(
				branchName,
				workingFolder,
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


		public string CommitMessage => viewModel.Message;

		public IReadOnlyList<CommitFile> CommitFiles => viewModel.CommitFiles;
	}
}
