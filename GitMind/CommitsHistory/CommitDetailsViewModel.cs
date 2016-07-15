using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class CommitDetailsViewModel : ViewModel
	{
		private readonly Func<string, Task> showDiffAsync;
		private readonly ObservableCollection<CommitFileViewModel> files = 
			new ObservableCollection<CommitFileViewModel>();
		private string filesCommitId = null;
		private CommitViewModel commitViewModel;


		public CommitDetailsViewModel(
			Func<string, Task> showDiffAsync)
		{
			this.showDiffAsync = showDiffAsync;
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
		public string Branch => CommitViewModel?.Commit?.Branch?.Name;
		public Brush BranchBrush => CommitViewModel?.Brush;
		public string Subject => CommitViewModel?.Subject;
		public Brush SubjectBrush => CommitViewModel?.SubjectBrush;
		public FontStyle SubjectStyle => CommitViewModel?.SubjectStyle ?? FontStyles.Normal;
		public string Tags => CommitViewModel?.Tags;
		public string Tickets => CommitViewModel?.Tickets;
		public string BranchTips => CommitViewModel?.BranchTips;

		public Command ShowDiffCommand => Command(ShowDiffAsync);


		public override string ToString() => $"{Id} {Subject}";


		private async void ShowDiffAsync()
		{
			await showDiffAsync(Id);
		}


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
						WorkingFolder = commit.GitRepositoryPath
					}));
			}
		}
	}
}