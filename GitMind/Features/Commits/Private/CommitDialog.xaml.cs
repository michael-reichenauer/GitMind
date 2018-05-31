using System;
using System.Collections.Generic;
using System.Windows;
using GitMind.Common.Tracking;
using GitMind.GitModel;
using GitMind.MainWindowViews;
using GitMind.Utils.Git;


namespace GitMind.Features.Commits.Private
{
	/// <summary>
	/// Interaction logic for CommitDialog.xaml
	/// </summary>
	public partial class CommitDialog : Window
	{
		private readonly CommitDialogViewModel viewModel;

		internal CommitDialog(
			WindowOwner owner,
			Func<
				CommitDialog,
				BranchName,
				IEnumerable<CommitFile>,
				string,
				bool,
				CommitDialogViewModel> CommitDialogViewModelProvider,
			BranchName branchName,

			IEnumerable<CommitFile> files,
			string commitMessage,
			bool isMerging)
		{
			Owner = owner;
			InitializeComponent();
			viewModel = CommitDialogViewModelProvider(
				this,
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
