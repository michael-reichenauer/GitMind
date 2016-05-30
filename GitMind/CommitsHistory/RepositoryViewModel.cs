using System.Collections.Generic;
using System.Collections.ObjectModel;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class RepositoryViewModel : ViewModel
	{
		public Repository Repository { get; set; }
		private int width = 0;
		private int graphWidth = 0;

		public List<BranchViewModel> Branches { get; } = new List<BranchViewModel>();
		public List<MergeViewModel> Merges { get; } = new List<MergeViewModel>();
		public List<CommitViewModel> Commits { get; } = new List<CommitViewModel>();
		public Dictionary<string, CommitViewModel> CommitsById { get; } =
			new Dictionary<string, CommitViewModel>();


		public RepositoryViewModel()
		{
			VirtualItemsSource = new RepositoryVirtualItemsSource(Branches, Merges, Commits);
		}


		public RepositoryVirtualItemsSource VirtualItemsSource { get; }

		public ObservableCollection<Branch> ActiveBranches { get; }
			= new ObservableCollection<Branch>();


		public CommitDetailViewModel CommitDetail { get; } = new CommitDetailViewModel(null);


		public int Width
		{
			get { return width; }
			set
			{
				if (width != value)
				{
					width = value;
					Commits.ForEach(commit => commit.WindowWidth = width);
				}

			}
		}

		public int GraphWidth
		{
			get { return graphWidth; }
			set
			{
				if (graphWidth != value)
				{
					graphWidth = value;
					Commits.ForEach(commit => commit.GraphWidth = graphWidth);
				}

			}
		}


		public int SelectedIndex
		{
			get { return Get(); }
			set
			{
				Log.Debug($"Setting value {value}");
				CommitViewModel commit = Commits[value];

				CommitDetail.Id = commit.Id;
				CommitDetail.Branch = commit.Commit.Branch.Name;
				CommitDetail.Tickets = commit.Tickets;
				CommitDetail.Tags = commit.Tags;
				CommitDetail.Subject = commit.Subject;
			}
		}

	}
}