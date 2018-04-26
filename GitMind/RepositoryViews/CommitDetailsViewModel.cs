using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.Common;
using GitMind.Common.ThemeHandling;
using GitMind.Features.Commits;
using GitMind.Features.Diffing;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitDetailsViewModel : ViewModel
	{
		private readonly IDiffService diffService;
		private readonly IThemeService themeService;
		private readonly ICommitsService commitsService;

		private readonly ObservableCollection<CommitFileViewModel> files =
			new ObservableCollection<CommitFileViewModel>();

		private CommitId filesCommitId = null;
		private CommitViewModel commitViewModel;


		public CommitDetailsViewModel(
			IDiffService diffService,
			IThemeService themeService,
			ICommitsService commitsService)
		{
			this.diffService = diffService;
			this.themeService = themeService;
			this.commitsService = commitsService;
		}


		public CommitViewModel CommitViewModel
		{
			get { return commitViewModel; }
			set
			{
				if (value != commitViewModel)
				{
					commitViewModel = value;
					Message = CommitViewModel?.Commit.Subject;

					NotifyAll();
				}

				NotifyAll();
			}
		}

		public ObservableCollection<CommitFileViewModel> Files
		{
			get
			{
				SetDetails();

				return files;
			}
		}


		private void SetDetails()
		{
			if (CommitViewModel != null)
			{
				if (filesCommitId != CommitViewModel.Commit.RealCommitId
					|| filesCommitId == Common.CommitId.Uncommitted)
				{
					files.Clear();
					filesCommitId = CommitViewModel.Commit.RealCommitId;
					SetFilesAsync(commitViewModel.Commit).RunInBackground();
				}
			}
			else
			{
				files.Clear();
				filesCommitId = null;
			}
		}


		//public string Message => CommitViewModel?.Commit.Subject;
		public string Message { get => Get(); set => Set(value); }

		public string CommitId => CommitViewModel?.Commit.RealCommitSha.Sha;
		public string ShortId => CommitViewModel?.ShortId;
		public string BranchName => CommitViewModel?.Commit?.Branch?.Name;
		public FontStyle BranchNameStyle => !string.IsNullOrEmpty(SpecifiedBranchName)
			? FontStyles.Oblique : FontStyles.Normal;
		public string BranchNameUnderline => !string.IsNullOrEmpty(SpecifiedBranchName) ? "Underline" : "None";
		public string BranchNameToolTip => SpecifiedBranchName != null ? "Manually specified branch" : null;
		public string SpecifiedBranchName => CommitViewModel?.Commit?.SpecifiedBranchName;
		public Brush BranchBrush => CommitViewModel?.Brush;
		public Brush SubjectBrush => CommitViewModel?.SubjectBrush;
		public FontStyle SubjectStyle => FontStyles.Normal;
		public ObservableCollection<LinkItem> Tags => CommitViewModel?.Tags;
		public ObservableCollection<LinkItem> Tickets => CommitViewModel?.Tickets;
		public bool HasTickets => CommitViewModel?.Tickets?.Any() ?? false;
		public bool HasTags => CommitViewModel?.Tags?.Any() ?? false;
		public string BranchTips => CommitViewModel?.BranchTips;

		public Command EditBranchCommand => CommitViewModel.SetCommitBranchCommand;
		public Command<string> UndoUncommittedFileCommand => Command<string>(
			path => commitsService.UndoUncommittedFileAsync(path));
		public Command ShowCommitDiffCommand => CommitViewModel?.ShowCommitDiffCommand;
		public Command CopyIdCommand => Command(() => System.Windows.Forms.Clipboard.SetText(CommitId));

		public override string ToString() => $"{CommitId} {CommitViewModel?.Commit.Subject}";

		private async Task SetFilesAsync(Commit commit)
		{
			CommitDetails commitDetails = await commit.FilesTask;
			if (commitDetails.Message != null)
			{
				Message = commitDetails.Message;
			}

			IEnumerable<CommitFile> commitFiles = commitDetails.Files;
			if (filesCommitId == commit.RealCommitId)
			{
				files.Clear();
				commitFiles
					.OrderBy(f => f.Status, Comparer<GitFileStatus>.Create(Compare))
					.ThenBy(f => f.Path)
					.ForEach(f => files.Add(
						new CommitFileViewModel(diffService, themeService, f, UndoUncommittedFileCommand)
						{
							Id = commit.RealCommitSha,
							Name = f.Path,
							Status = f.StatusText,
							WorkingFolder = commit.WorkingFolder
						}));
			}
		}


		private static int Compare(GitFileStatus s1, GitFileStatus s2)
		{
			if (s1.HasFlag(GitFileStatus.Conflict) && !s2.HasFlag(GitFileStatus.Conflict))
			{
				return -1;
			}
			else if (s2.HasFlag(GitFileStatus.Conflict) && !s1.HasFlag(GitFileStatus.Conflict))
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}
	}
}