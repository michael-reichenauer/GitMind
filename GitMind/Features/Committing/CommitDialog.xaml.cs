using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils.UI;


namespace GitMind.Features.Committing
{
	/// <summary>
	/// Interaction logic for CommitDialog.xaml
	/// </summary>
	public partial class CommitDialog : Window
	{
		private readonly CommitDialogViewModel viewModel;

		internal CommitDialog(
			Window owner,
			IRepositoryCommands repositoryCommands,
			string branchName,
			string workingFolder,
			IEnumerable<CommitFile> files,
			string commitMessage,
			bool isMerging)
		{
			Owner = owner;
			InitializeComponent();
			viewModel = new CommitDialogViewModel(
				repositoryCommands,
				branchName,
				workingFolder,
				files,
				commitMessage,
				isMerging);

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
