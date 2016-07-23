using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitDetailsViewModel : ViewModel
	{
		private readonly ObservableCollection<CommitFileViewModel> files =
			new ObservableCollection<CommitFileViewModel>();
		private string filesCommitId = null;
		private CommitViewModel commitViewModel;



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
			}
		}

		public ObservableCollection<CommitFileViewModel> Files
		{
			get
			{
				if (CommitViewModel != null)
				{
					if (filesCommitId != CommitViewModel.Id)
					{
						files.Clear();
						filesCommitId = CommitViewModel.Id;
						SetFilesAsync(commitViewModel.Commit).RunInBackground();
					}
				}
				else
				{
					files.Clear();
					filesCommitId = null;
				}

				return files;
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
		public string Subject => CommitViewModel?.Subject;
		public Brush SubjectBrush => CommitViewModel?.SubjectBrush;
		public FontStyle SubjectStyle => CommitViewModel?.SubjectStyle ?? FontStyles.Normal;
		public string Tags => CommitViewModel?.Tags;
		public string Tickets => CommitViewModel?.Tickets;
		public string BranchTips => CommitViewModel?.BranchTips;

		public Command EditBranchCommand => CommitViewModel.SetCommitBranchCommand;


		public override string ToString() => $"{Id} {Subject}";



		private async Task SetFilesAsync(Commit commit)
		{
			IEnumerable<CommitFile> commitFiles = await commit.FilesTask;
			if (filesCommitId == commit.Id)
			{
				files.Clear();
				commitFiles.ForEach(f => files.Add(
					new CommitFileViewModel
					{
						Id = commit.Id,
						Name = f.Name,
						Status = f.Status,
						WorkingFolder = commit.WorkingFolder
					}));
			}
		}
	}
}