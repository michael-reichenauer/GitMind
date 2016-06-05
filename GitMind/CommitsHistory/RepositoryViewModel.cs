using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class RepositoryViewModel : ViewModel
	{
		private readonly IViewModelService viewModelService;

		private readonly DispatcherTimer filterTriggerTimer = new DispatcherTimer();
		private string settingFilterText = "";
		private string filterText = "";

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

			filterTriggerTimer.Tick += FilterTrigger;
			filterTriggerTimer.Interval = TimeSpan.FromMilliseconds(300);
		}


		public Repository Repository { get; private set; } 

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

		public void Update(Repository repository)
		{
			Repository = repository;
			viewModelService.Update(this, repository);
			Commits.ForEach(commit => commit.WindowWidth = Width);
			VirtualItemsSource.DataChanged(width);
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
		public IReadOnlyList<Branch> SpecifiedBranches { get; set; }


		public void SetFilter(string text)
		{
			filterTriggerTimer.Stop();
			settingFilterText = (text ?? "").Trim();
			filterTriggerTimer.Start();
		}


		private void FilterTrigger(object sender, EventArgs e)
		{
			filterTriggerTimer.Stop();
			filterText = settingFilterText;

			Log.Debug($"Filter: {filterText}");
			viewModelService.SetFilter(this, filterText);

			VirtualItemsSource.DataChanged(width);
		}


		public int Clicked(int column, int rowIndex, bool isControl)
		{
			if (rowIndex < 0 || rowIndex >= Commits.Count || column < 0 || column >= Branches.Count)
			{
				// Click is not within supported area
				return rowIndex;
			}

			CommitViewModel commitViewModel = Commits[rowIndex];

			if (commitViewModel.IsMergePoint && commitViewModel.BranchColumn == column)
			{
				// User clicked on a merge point (toggle between expanded and collapsed)
				int diff = viewModelService.ToggleMergePoint(this, commitViewModel.Commit);

				VirtualItemsSource.DataChanged(width);

				return rowIndex + diff;
			}

			return rowIndex;
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


		public Point Clicked(Point position, bool isControl)
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
				int newRow = Clicked(column, row, isControl);
				if (newRow != row)
				{
					return new Point(position.X, Converter.ToY(newRow) + 10);
				}
			}

			return position;
		}

	}
}