using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
		private static readonly TimeSpan FilterDelay = TimeSpan.FromMilliseconds(200);
		private readonly IViewModelService viewModelService;
		private readonly Lazy<BusyIndicator> busyIndicator;


		private readonly DispatcherTimer filterTriggerTimer = new DispatcherTimer();
		private string settingFilterText = "";


		private int width = 0;
		private int graphWidth = 0;

		public List<BranchViewModel> Branches { get; } = new List<BranchViewModel>();
		public List<MergeViewModel> Merges { get; } = new List<MergeViewModel>();
		public List<CommitViewModel> Commits { get; } = new List<CommitViewModel>();


		public Dictionary<string, CommitViewModel> CommitsById { get; } =
			new Dictionary<string, CommitViewModel>();


		public RepositoryViewModel(
			Action<int> setFirstVisibleRow,
			Lazy<BusyIndicator> busyIndicator)
			: this(new ViewModelService(), setFirstVisibleRow, busyIndicator)
		{
		}

		public RepositoryViewModel(
			IViewModelService viewModelService,
			Action<int> scrollRows,
			Lazy<BusyIndicator> busyIndicator)
		{
			ScrollRows = scrollRows;
			this.viewModelService = viewModelService;
			this.busyIndicator = busyIndicator;

			VirtualItemsSource = new RepositoryVirtualItemsSource(Branches, Merges, Commits);

			filterTriggerTimer.Tick += FilterTrigger;
			filterTriggerTimer.Interval = FilterDelay;
		}

		public Action<int> ScrollRows { get; }
		public Repository Repository { get; private set; }

		public ICommand ShowBranchCommand => Command<Branch>(ShowBranch);
		//public ICommand HideBranchCommand => Command<string>(HideBranch);
		public ICommand ToggleDetailsCommand => Command(ToggleDetails);


		public RepositoryVirtualItemsSource VirtualItemsSource { get; }

		public ObservableCollection<BranchName> AllBranches { get; }
			= new ObservableCollection<BranchName>();


		public CommitDetailViewModel CommitDetail { get; } = new CommitDetailViewModel(null);

		public string FilterText { get; private set; } = "";
		public string FilteredText { get; private set; } = "";

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
					Commits.ForEach(commit => commit.WindowWidth = width - 2);
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

		public void Update(Repository repository, IReadOnlyList<Branch> specifiedBranch)
		{
			Timing t = new Timing();
			Repository = repository;
			viewModelService.Update(this, specifiedBranch);
			Commits.ForEach(commit => commit.WindowWidth = Width);

			VirtualItemsSource.DataChanged(width);

			t.Log("Updated repository view model");
		}


		public void Update(Repository repository, IReadOnlyList<string> specifiedBranchNames)
		{
			Timing t = new Timing();
			Repository = repository;
			viewModelService.Update(this, specifiedBranchNames);
			Commits.ForEach(commit => commit.WindowWidth = Width);

			VirtualItemsSource.DataChanged(width);

			if (Commits.Any())
			{
				SelectedIndex = Commits[0].VirtualId;
			}

			t.Log("Updated repository view model");
		}


		public int SelectedIndex
		{
			get { return Get(); }
			set
			{
				if (Set(value).IsSet)
				{
				}
			}
		}


		public object SelectedItem
		{
			get { return Get().Value; }
			set
			{
				if (Set(value).IsSet)
				{
					CommitViewModel commit = value as CommitViewModel;
					if (commit != null)
					{
						CommitDetail.Id = commit.Id;
						CommitDetail.Branch = commit.Commit.Branch.Name;
						CommitDetail.Tickets = commit.Tickets;
						CommitDetail.Tags = commit.Tags;
						CommitDetail.Subject = commit.Subject;
						CommitDetail.Files.Clear();
						commit.Commit.Files.ForEach(f => CommitDetail.Files.Add(
							new CommitFileViewModel { Id = commit.Id, Name = f.Name, Status = f.Status}));
					}
				}
			}
		}



		public IReadOnlyList<Branch> SpecifiedBranches { get; set; }


		public void SetFilter(string text)
		{
			filterTriggerTimer.Stop();
			Log.Debug($"Filter: {text}");
			settingFilterText = (text ?? "").Trim();
			filterTriggerTimer.Start();
		}


		private async void FilterTrigger(object sender, EventArgs e)
		{
			filterTriggerTimer.Stop();
			string filterText = settingFilterText;
			FilterText = filterText;

			Log.Debug($"Filter triggered for: {FilterText}");

			CommitViewModel selectedBefore = (CommitViewModel)SelectedItem;
			int indexBefore = Commits.FindIndex(c => c == selectedBefore);

			Task setFilterTask = viewModelService.SetFilterAsync(this);
			busyIndicator.Value.Add(setFilterTask);

			await setFilterTask;
			if (filterText != FilterText)
			{
				Log.Warn($"Filter has changed {filterText} ->" + $"{FilterText}");
				return;
			}
			FilteredText = filterText;
			int indexAfter = Commits.FindIndex(c => c == selectedBefore);

			Log.Debug($"Selected {indexBefore}->{indexAfter} for commit {selectedBefore}");
			if (indexBefore != -1 && indexAfter != -1)
			{
				ScrollRows(indexBefore - indexAfter);
			}

			VirtualItemsSource.DataChanged(width);
		}


		public void Clicked(int column, int rowIndex, bool isControl)
		{
			if (rowIndex < 0 || rowIndex >= Commits.Count || column < 0 || column >= Branches.Count)
			{
				// Click is not within supported area
				return;
			}

			CommitViewModel commitViewModel = Commits[rowIndex];

			if (commitViewModel.IsMergePoint && commitViewModel.BranchColumn == column)
			{
				// User clicked on a merge point (toggle between expanded and collapsed)
				int rowsChange = viewModelService.ToggleMergePoint(this, commitViewModel.Commit);

				ScrollRows(rowsChange);
				VirtualItemsSource.DataChanged(width);
			}
		}


		private void ShowBranch(Branch branch)
		{
			viewModelService.ShowBranch(this, branch);
		}


		//private void HideBranch(string obj)
		//{
		//	throw new System.NotImplementedException();
		//}


		private void ToggleDetails()
		{
			DetailsSize = DetailsSize > 0 ? 0 : 150;
		}


		public void Clicked(Point position, bool isControl)
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
				Clicked(column, row, isControl);

			}
		}
	}
}