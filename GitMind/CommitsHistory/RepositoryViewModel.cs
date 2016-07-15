using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.UI;
using GitMind.VirtualCanvas;


namespace GitMind.CommitsHistory
{
	internal class RepositoryViewModel : ViewModel
	{
		public Lazy<BusyIndicator> Busy { get; set; }
		private static readonly TimeSpan FilterDelay = TimeSpan.FromMilliseconds(300);
		private readonly IViewModelService viewModelService;
		
		private readonly IRepositoryService repositoryService = new RepositoryService();


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
			Lazy<BusyIndicator> busyIndicator)
			: this(new ViewModelService(), busyIndicator)
		{
			Busy = busyIndicator;
		}


		public RepositoryViewModel(
			IViewModelService viewModelService,
			Lazy<BusyIndicator> busyIndicator)
		{
			this.viewModelService = viewModelService;


			VirtualItemsSource = new RepositoryVirtualItemsSource(Branches, Merges, Commits);

			filterTriggerTimer.Tick += FilterTrigger;
			filterTriggerTimer.Interval = FilterDelay;
		}


		public Repository Repository { get; private set; }

		public ICommand ShowBranchCommand => Command<Branch>(ShowBranch);
		public ICommand HideBranchCommand => Command<Branch>(HideBranch);

		public ICommand SpecifyMultiBranchCommand => Command<string>(SpecifyMultiBranch);


		private async void SpecifyMultiBranch(string text)
		{
			string[] parts = text.Split(",".ToCharArray());

			string gitRepositoryPath = Repository.MRepository.WorkingFolder;
			await repositoryService.SetSpecifiedCommitBranchAsync(parts[0], parts[1], gitRepositoryPath);
		}


		public Commit UnCommited
		{
			get { return Get<Commit>(); }
			set { Set(value); }
		}


		public string RemoteAheadText
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string LocalAheadText
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string ConflictAheadText
		{
			get { return Get(); }
			set { Set(value); }
		}


		public ICommand ToggleDetailsCommand => Command(ToggleDetails);


		public RepositoryVirtualItemsSource VirtualItemsSource { get; }

		public IReadOnlyList<BranchItem> AllBranches => BranchItem.GetBranches(
			Repository.Branches
			.Where(b => b.IsActive && b.Name != "master")
			.Where(b => !ActiveBranches.Any(ab => ab.Branch.Id == b.Id)),
			ShowBranchCommand);			

		public ObservableCollection<BranchItem> ActiveBranches { get; }
			= new ObservableCollection<BranchItem>();


		public CommitDetailsViewModel CommitDetailsViewModel { get; } = new CommitDetailsViewModel(null);

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
			if (string.IsNullOrEmpty(FilterText) && string.IsNullOrEmpty(settingFilterText))
			{
				viewModelService.Update(this, specifiedBranch);
				Commits.ForEach(commit => commit.WindowWidth = Width);

				VirtualItemsSource.DataChanged(width);

				if (Commits.Any())
				{
					SelectedIndex = Commits[0].VirtualId;
					SetCommitsDetails(Commits[0]);
				}

				t.Log("Updated repository view model");
			}
			else
			{
				Log.Debug("Not updating while in filter mode");
			}

			UpdateStatusIndicators();
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
				SetCommitsDetails(Commits[0]);
			}

			UpdateStatusIndicators();

			t.Log("Updated repository view model");
		}


		private void UpdateStatusIndicators()
		{
			IEnumerable<Branch> remoteAheadBranches = Repository.Branches
				.Where(b => b.RemoteAheadCount > 0).ToList();
		
			string remoteAheadText = remoteAheadBranches.Any() 
				? "Branches with remote commits:\n" : null;
			foreach (Branch branch in remoteAheadBranches)
			{
				remoteAheadText += $"\n    {branch.RemoteAheadCount}\t{branch.Name}";
			}

			RemoteAheadText = remoteAheadText;

			IEnumerable<Branch> localAheadBranches = Repository.Branches
				.Where(b => b.LocalAheadCount > 0).ToList();
		
			string localAheadText = localAheadBranches.Any()
				? "Branches with local commits:\n" : null;
			foreach (Branch branch in localAheadBranches)
			{
				localAheadText += $"\n    {branch.LocalAheadCount}\t{branch.Name}";
			}

			LocalAheadText = localAheadText;

			Commit uncommitted;
			Repository.Commits.TryGetValue(Commit.UncommittedId, out uncommitted);
			UnCommited = uncommitted;
		}


		public int SelectedIndex
		{
			get { return Get(); }
			set { Set(value); }
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
						SetCommitsDetails(commit);
					}
				}
			}
		}


		private void SetCommitsDetails(CommitViewModel commit)
		{
			CommitDetailsViewModel.CommitViewModel = commit;		
		}



		public IReadOnlyList<Branch> SpecifiedBranches { get; set; }
		public ZoomableCanvas Canvas { get; set; }
		public string WorkingFolder { get; set; }
		public List<string> SpecifiedBranchNames { get; set; }


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

			CommitViewModel selectedBefore = SelectedItem as CommitViewModel;
			int indexBefore = Commits.FindIndex(c => c == selectedBefore);

			Task setFilterTask = viewModelService.SetFilterAsync(this, filterText);
			Busy.Value.Add(setFilterTask);

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
			else
			{
				ScrollTo(0);
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


		public void ScrollRows(int rows)
		{
			int offsetY = Converter.ToY(rows);
			Canvas.Offset = new Point(Canvas.Offset.X, Math.Max(Canvas.Offset.Y - offsetY, 0));
		}


		private void ScrollTo(int rows)
		{
			int offsetY = Converter.ToY(rows);
			Canvas.Offset = new Point(Canvas.Offset.X, Math.Max(offsetY, 0));
		}


		private void ShowBranch(Branch branch)
		{
			viewModelService.ShowBranch(this, branch);
		}


		private void HideBranch(Branch branch)
		{
			viewModelService.HideBranch(this, branch);
		}


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