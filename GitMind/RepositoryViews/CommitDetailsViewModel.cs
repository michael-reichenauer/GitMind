using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.Features.Committing;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitDetailsViewModel : ViewModel
	{
		private readonly ICommitService commitService = new CommitService();
		private readonly IGitService gitService = new GitService();
		private readonly IRepositoryCommands repositoryCommands;

		private readonly ObservableCollection<CommitFileViewModel> files =
			new ObservableCollection<CommitFileViewModel>();
		private string filesCommitId = null;
		private CommitViewModel commitViewModel;


		public CommitDetailsViewModel(IRepositoryCommands repositoryCommands)
		{
			this.repositoryCommands = repositoryCommands;
		}


		public CommitViewModel CommitViewModel
		{
			get { return commitViewModel; }
			set
			{
				if (value != commitViewModel)
				{
					commitViewModel = value;
					NotifyAll();
				}

				NotifyAll();
			}
		}

		public ObservableCollection<CommitFileViewModel> Files
		{
			get
			{
				SetFiles();

				return files;
			}
		}


		private void SetFiles()
		{
			if (CommitViewModel != null)
			{
				if (filesCommitId != CommitViewModel.Commit.CommitId 
					|| filesCommitId == Commit.UncommittedId)
				{
					files.Clear();
					filesCommitId = CommitViewModel.Commit.CommitId;
					SetFilesAsync(commitViewModel.Commit).RunInBackground();
				}
			}
			else
			{
				files.Clear();
				filesCommitId = null;
			}
		}


		public string Subject
		{
			get
			{
				string subject = CommitViewModel?.Subject;
				if (CommitViewModel != null)
				{
					string workingFolder = CommitViewModel.Commit.WorkingFolder;
					string commitId = CommitViewModel.Commit.CommitId;
					subject = gitService.GetFullMessage(workingFolder, commitId) ?? CommitViewModel?.Subject;
				}

				return subject;
			}
		}

		public string Id => CommitViewModel?.Id;
		public string ShortId => CommitViewModel?.ShortId;
		public string BranchName => CommitViewModel?.Commit?.Branch?.Name;
		public FontStyle BranchNameStyle => !string.IsNullOrEmpty(SpecifiedBranchName)
			? FontStyles.Oblique : FontStyles.Normal;
		public string BranchNameUnderline => !string.IsNullOrEmpty(SpecifiedBranchName) ? "Underline" : "None";
		public string BranchNameToolTip => SpecifiedBranchName != null ? "Manually specified branch" : null;
		public string SpecifiedBranchName => CommitViewModel?.Commit?.SpecifiedBranchName;
		public Brush BranchBrush => CommitViewModel?.Brush;
		public Brush SubjectBrush => CommitViewModel?.SubjectBrush;
		public FontStyle SubjectStyle => CommitViewModel?.SubjectStyle ?? FontStyles.Normal;
		public string Tags => CommitViewModel?.Tags;
		public string Tickets => CommitViewModel?.Tickets;
		public string BranchTips => CommitViewModel?.BranchTips;

		public Command EditBranchCommand => CommitViewModel.SetCommitBranchCommand;
		public Command<string> UndoUncommittedFileCommand => Command<string>(
			path => commitService.UndoUncommittedFileAsync(repositoryCommands, path));
		public Command ShowCommitDiffCommand => CommitViewModel.ShowCommitDiffCommand;

		public override string ToString() => $"{Id} {Subject}";

		private async Task SetFilesAsync(Commit commit)
		{
			IEnumerable<CommitFile> commitFiles = await commit.FilesTask;
			if (filesCommitId == commit.CommitId)
			{
				files.Clear();
				commitFiles
					.OrderBy(f => f.Status, 
					Comparer<GitFileStatus>.Create(
						(s1, s2) => s1 == GitFileStatus.Conflict ? 1 : s2 == GitFileStatus.Conflict ? 1 : 0))
					.ForEach(f => files.Add(
					new CommitFileViewModel(f, UndoUncommittedFileCommand)
					{
						Id = commit.CommitId,
						Name = f.Path,
						Status = f.StatusText,
						WorkingFolder = commit.WorkingFolder
					}));
			}
		}
	}
}