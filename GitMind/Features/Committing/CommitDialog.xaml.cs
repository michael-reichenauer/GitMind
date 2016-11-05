using System;
using System.Collections.Generic;
using System.Windows;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.RepositoryViews;


namespace GitMind.Features.Committing
{
	/// <summary>
	/// Interaction logic for CommitDialog.xaml
	/// </summary>
	public partial class CommitDialog : Window
	{
		private readonly CommitDialogViewModel viewModel;

		internal CommitDialog(
			Func<
				CommitDialog,
				IRepositoryCommands,
				BranchName,
				IEnumerable<CommitFile>,
				string,
				bool,
				CommitDialogViewModel> CommitDialogViewModelProvider,
			IRepositoryCommands repositoryCommands,
			BranchName branchName,

			IEnumerable<CommitFile> files,
			string commitMessage,
			bool isMerging)
		{
			Owner = repositoryCommands.Owner;
			InitializeComponent();
			viewModel = CommitDialogViewModelProvider(
				this,
				repositoryCommands,
				branchName,
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

		public bool IsChanged => viewModel.IsChanged;

		public IReadOnlyList<CommitFile> CommitFiles => viewModel.CommitFiles;
	}
}
