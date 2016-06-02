using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class RepositoryViewModel : ViewModel
	{
		private readonly IViewModelService viewModelService;

		public Repository Repository { get; set; }
		private int width = 0;
		private int graphWidth = 0;

		public List<BranchViewModel> Branches { get; } = new List<BranchViewModel>();
		public List<MergeViewModel> Merges { get; } = new List<MergeViewModel>();
		public List<CommitViewModel> Commits { get; } = new List<CommitViewModel>();
		public Dictionary<string, CommitViewModel> CommitsById { get; } =
			new Dictionary<string, CommitViewModel>();


		public RepositoryViewModel()
			: this(new ViewModelService())
		{		
		}

		public RepositoryViewModel(IViewModelService viewModelService)
		{
			this.viewModelService = viewModelService;
			VirtualItemsSource = new RepositoryVirtualItemsSource(Branches, Merges, Commits);
		}


		public void Update(Repository repository)
		{
			viewModelService.Update(this, repository);
			Commits.ForEach(commit => commit.WindowWidth = Width);
			VirtualItemsSource.DataChanged(width);
		}


		public ICommand ShowBranchCommand => Command<string>(ShowBranch);
		public ICommand HideBranchCommand => Command<string>(HideBranch);
		public ICommand ToggleDetailsCommand => Command(ToggleDetails);


		public RepositoryVirtualItemsSource VirtualItemsSource { get; }

		public ObservableCollection<Branch> ActiveBranches { get; }
			= new ObservableCollection<Branch>();


		public CommitDetailViewModel CommitDetail { get; } = new CommitDetailViewModel(null);

		public int DetailsSize
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int Width
		{
			get { return width; }
			set
			{
				if (width != value)
				{
					width = value;
					Commits.ForEach(commit => commit.WindowWidth = width);
					VirtualItemsSource.DataChanged(width);
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


		public async Task ToggleAsync(int column, int rowIndex, bool isControl)
		{
			// Log.Debug($"Clicked at {column},{rowIndex}");
			if (rowIndex < 0 || rowIndex >= Commits.Count || column < 0 || column >= Branches.Count)
			{
				// Not within supported area
				return;
			}

			CommitViewModel commitViewModel = Commits[rowIndex];

			if (commitViewModel.IsMergePoint && commitViewModel.BranchColumn == column)
			{
				// User clicked on a merge point (toggle between expanded and collapsed)
				Log.Debug($"Clicked at {column},{rowIndex}, {commitViewModel}");

				viewModelService.Toggle(this, commitViewModel.Commit);
				VirtualItemsSource.DataChanged(width);
			}

			//if (isControl && commitViewModel.Commit.Id == commitViewModel.Commit.Branch.LatestCommit.Id
			//	&& activeBrancheNames.Count > 1)
			//{
			//	// User clicked on latest commit point on a branch, which will close the branch 
			//	activeBrancheNames.Remove(commitViewModel.Commit.Branch.Name);

			//	UpdateUIModel();
			//}

			await Task.Delay(1);
		}



		private void ShowBranch(string obj)
		{
			throw new System.NotImplementedException();
		}


		private void HideBranch(string obj)
		{
			throw new System.NotImplementedException();
		}


		private void ToggleDetails()
		{
			DetailsSize = DetailsSize > 0 ? 0 : 150;
		}


		public async Task ClickedAsync(Point position, bool isControl)
		{
			double xpos = position.X - 9;
			double ypos = position.Y - 5;

			int column = Converter.ToColumn(xpos);
			int x = Converter.ToX(column);

			int row = Converter.ToRow(ypos);
			int y = Converter.ToY(row) + 10;

			double absx = Math.Abs(xpos - x);
			double absy = Math.Abs(ypos - y);

			if ((absx < 10) && (absy < 10))
			{
				await ToggleAsync(column, row, isControl);
			}
		}

	}
}