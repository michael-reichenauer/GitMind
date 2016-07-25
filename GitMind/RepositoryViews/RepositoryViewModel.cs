using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.UI;
using GitMind.Utils.UI.VirtualCanvas;


namespace GitMind.RepositoryViews
{
	internal class RepositoryViewModel : ViewModel
	{
		private static readonly TimeSpan FilterDelay = TimeSpan.FromMilliseconds(300);

		private readonly IViewModelService viewModelService;
		private readonly IRepositoryService repositoryService = new RepositoryService();
		private readonly IGitService gitService = new GitService();
		private readonly IBrushService brushService = new BrushService();

		private readonly BusyIndicator busyIndicator;


		private readonly DispatcherTimer filterTriggerTimer = new DispatcherTimer();
		private string settingFilterText = "";

		private int width = 0;
		private int graphWidth = 0;

		public List<BranchViewModel> Branches { get; } = new List<BranchViewModel>();
		public List<MergeViewModel> Merges { get; } = new List<MergeViewModel>();
		public List<CommitViewModel> Commits { get; } = new List<CommitViewModel>();


		public Dictionary<string, CommitViewModel> CommitsById { get; } =
			new Dictionary<string, CommitViewModel>();

		private DateTime fetchedTime = DateTime.MinValue;
		private DateTime RebuildRepositoryTime = DateTime.MinValue;
		private static readonly TimeSpan FetchInterval = TimeSpan.FromMinutes(10);
		private readonly TaskThrottler refreshThrottler = new TaskThrottler(1);


		public RepositoryViewModel(
			BusyIndicator busyIndicator, ICommand refreshManuallyCommand)
			: this(new ViewModelService(refreshManuallyCommand), busyIndicator)
		{
		}


		public IReadOnlyList<Branch> SpecifiedBranches { get; set; }
		public string WorkingFolder { get; set; }
		public IReadOnlyList<string> SpecifiedBranchNames { get; set; }
		public ZoomableCanvas Canvas { get; set; }


		public RepositoryViewModel(
			IViewModelService viewModelService,
			BusyIndicator busyIndicator)
		{
			this.viewModelService = viewModelService;
			this.busyIndicator = busyIndicator;


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


		public string CurrentBranchName
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Brush CurrentBranchBrush
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string PullCurrentBranchText => $"Pull current branch {CurrentBranchName}";

		public string PushCurrentBranchText => $"Push current branch {CurrentBranchName}";


		public ICommand ToggleDetailsCommand => Command(ToggleDetails);

		public ICommand TryUpdateAllBranchesCommand => Command(
			TryUpdateAllBranches, TryUpdateAllBranchesCanExecute);

		public ICommand PullCurrentBranchCommand => Command(
			PullCurrentBranch, PullCurrentBranchCanExecute);


		public ICommand TryPushAllBranchesCommand => Command(
			TryPushAllBranches, TryPushAllBranchesCanExecute);

		public ICommand PushCurrentBranchCommand => Command(
			PushCurrentBranch, PushCurrentBranchCanExecute);





		public ICommand ShowCurrentBranchCommand => Command(ShowCurrentBranch);


		public RepositoryVirtualItemsSource VirtualItemsSource { get; }

		public IReadOnlyList<BranchItem> AllBranches => BranchItem.GetBranches(
			Repository.Branches
			.Where(b => b.IsActive && b.Name != "master")
			.Where(b => !ActiveBranches.Any(ab => ab.Branch.Id == b.Id)),
			ShowBranchCommand);

		public ObservableCollection<BranchItem> ActiveBranches { get; }
			= new ObservableCollection<BranchItem>();


		public CommitDetailsViewModel CommitDetailsViewModel { get; } = new CommitDetailsViewModel();

		public string FilterText { get; private set; } = "";
		public string FilteredText { get; private set; } = "";

		public bool IsShowCommitDetails
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


		public Task FirstLoadAsync()
		{
			return refreshThrottler.Run(async () =>
			{
				Log.Debug("Loading repository");

				using (busyIndicator.Progress)
				{
					Repository repository = await repositoryService.GetCachedOrFreshRepositoryAsync(WorkingFolder);
					UpdateViewModel(repository, SpecifiedBranchNames);

					repository = await GetLocalChangesAsync(Repository);
					UpdateViewModel(repository, SpecifiedBranches);

					await FetchRemoteChangesAsync(Repository);
					repository = await GetLocalChangesAsync(Repository);
					UpdateViewModel(repository, SpecifiedBranches);
				}
			});
		}


		public Task ActivateRefreshAsync()
		{
			return refreshThrottler.Run(async () =>
			{
				Log.Debug("Refresh after activating");

				Repository repository;

				using (busyIndicator.Progress)
				{
					repository = await GetLocalChangesAsync(Repository);
					UpdateViewModel(repository, SpecifiedBranches);
				}

				if (DateTime.Now - fetchedTime > FetchInterval)
				{
					await FetchRemoteChangesAsync(Repository);
				}

				repository = await GetLocalChangesAsync(Repository);
				UpdateViewModel(repository, SpecifiedBranches);
			});
		}



		public Task AutoRefreshAsync()
		{
			return refreshThrottler.Run(async () =>
			{
				Log.Debug("Auto refresh");

				if (DateTime.Now - fetchedTime > FetchInterval)
				{
					await FetchRemoteChangesAsync(Repository);
				}

				Repository repository;
				if (DateTime.Now - RebuildRepositoryTime > TimeSpan.FromMinutes(10))
				{
					Log.Debug("Get fresh repository from scratch");
					repository = await repositoryService.GetFreshRepositoryAsync(WorkingFolder);
					RebuildRepositoryTime = DateTime.Now;
				}
				else
				{
					repository = await GetLocalChangesAsync(Repository);
				}

				UpdateViewModel(repository, SpecifiedBranches);
			});
		}


		private Task RefreshAfterCommandAsync()
		{
			return refreshThrottler.Run(async () =>
			{
				Log.Debug("Auto refresh");

				await FetchRemoteChangesAsync(Repository);

				Repository repository = await GetLocalChangesAsync(Repository);

				UpdateViewModel(repository, SpecifiedBranches);
			});
		}



		public Task ManualRefreshAsync()
		{
			return refreshThrottler.Run(async () =>
			{
				Log.Debug("Manual refresh");

				Repository repository;
				using (busyIndicator.Progress)
				{
					await FetchRemoteChangesAsync(Repository);

					repository = await GetLocalChangesAsync(Repository);
					UpdateViewModel(repository, SpecifiedBranches);

					Log.Debug("Get fresh repository from scratch");
					repository = await repositoryService.GetFreshRepositoryAsync(WorkingFolder);
				}

				RebuildRepositoryTime = DateTime.Now;
				UpdateViewModel(repository, SpecifiedBranches);
			});
		}


		public void MouseEnterBranch(BranchViewModel branch)
		{
			branch.SetHighlighted();

			foreach (CommitViewModel commit in Commits)
			{
				if (commit.Commit.Branch.Id != branch.Branch.Id)
				{
					commit.SetDim();					
				}
			}
		}


		public void MouseLeaveBranch(BranchViewModel branch)
		{
			branch.SetNormal();

			foreach (CommitViewModel commit in Commits)
			{
				commit.SetNormal(viewModelService.GetSubjectBrush(commit.Commit));
			}
		}




		private Task<Repository> GetLocalChangesAsync(Repository repository)
		{
			return repositoryService.UpdateRepositoryAsync(repository);
		}


		private async Task FetchRemoteChangesAsync(Repository repository)
		{
			await gitService.FetchAsync(repository.MRepository.WorkingFolder);
			fetchedTime = DateTime.Now;
		}



		private void UpdateViewModel(Repository repository, IReadOnlyList<Branch> specifiedBranch)
		{
			Timing t = new Timing();
			Repository = repository;
			if (string.IsNullOrEmpty(FilterText) && string.IsNullOrEmpty(settingFilterText))
			{
				CommitPosition commitPosition = TryGetSelectedCommitPosition();

				viewModelService.UpdateViewModel(this, specifiedBranch);

				UpdateViewModel();

				TrySetSelectedCommitPosition(commitPosition);
				t.Log("Updated repository view model");
			}
		}


		private void UpdateViewModel(Repository repository, IReadOnlyList<string> specifiedBranchNames)
		{
			Timing t = new Timing();
			Repository = repository;
			viewModelService.Update(this, specifiedBranchNames);

			UpdateViewModel();

			if (Commits.Any())
			{
				SelectedIndex = 0;
				SelectedItem = Commits.First();
			}

			t.Log("Updated repository view model");
		}


		private void UpdateViewModel()
		{
			Commits.ForEach(commit => commit.WindowWidth = Width);
			CommitDetailsViewModel.NotifyAll();
			VirtualItemsSource.DataChanged(width);

			UpdateStatusIndicators();
		}


		private void UpdateStatusIndicators()
		{
			CurrentBranchName = Repository.CurrentBranch.Name;
			CurrentBranchBrush = brushService.GetBranchBrush(Repository.CurrentBranch);

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


		public void SetFilter(string text)
		{
			filterTriggerTimer.Stop();
			Log.Debug($"Filter: {text}");
			settingFilterText = (text ?? "").Trim();
			filterTriggerTimer.Start();
		}


		private class CommitPosition
		{
			public CommitPosition(Commit commit, int index)
			{
				Commit = commit;
				Index = index;
			}

			public Commit Commit { get; }
			public int Index { get; }
		}

		private async void FilterTrigger(object sender, EventArgs e)
		{
			filterTriggerTimer.Stop();
			string filterText = settingFilterText;
			FilterText = filterText;

			Log.Debug($"Filter triggered for: {FilterText}");

			CommitPosition commitPosition = TryGetSelectedCommitPosition();

			using (busyIndicator.Progress)
			{
				await viewModelService.SetFilterAsync(this, filterText);
			}

			if (filterText != FilterText)
			{
				Log.Warn($"Filter has changed {filterText} ->" + $"{FilterText}");
				return;
			}

			FilteredText = filterText;
			TrySetSelectedCommitPosition(commitPosition);
			CommitDetailsViewModel.NotifyAll();

			VirtualItemsSource.DataChanged(width);
		}


		private CommitPosition TryGetSelectedCommitPosition()
		{
			Commit selected = (SelectedItem as CommitViewModel)?.Commit;
			int index = -1;

			if (selected != null)
			{
				index = Commits.FindIndex(c => c.Commit.Id == selected.Id);
			}

			if (selected != null && index != -1)
			{
				return new CommitPosition(selected, index);
			}

			return null;
		}


		private void TrySetSelectedCommitPosition(CommitPosition commitPosition)
		{
			if (commitPosition != null)
			{
				Commit selected = commitPosition.Commit;

				int indexAfter = Commits.FindIndex(c => c.Commit.Id == selected.Id);

				if (selected != null && indexAfter != -1)
				{
					int indexBefore = commitPosition.Index;
					ScrollRows(indexBefore - indexAfter);
					SelectedIndex = indexAfter;
					SelectedItem = Commits[indexAfter];
					return;
				}
			}

			ScrollTo(0);
			if (Commits.Any())
			{
				SelectedIndex = 0;
				SelectedItem = Commits.First();
			}
		}

		public bool Clicked(int column, int rowIndex, bool isControl)
		{
			if (rowIndex < 0 || rowIndex >= Commits.Count || column < 0 || column >= Branches.Count)
			{
				// Click is not within supported area
				return false;
			}

			CommitViewModel commitViewModel = Commits[rowIndex];

			if (commitViewModel.IsMergePoint && commitViewModel.BranchColumn == column)
			{
				// User clicked on a merge point (toggle between expanded and collapsed)
				int rowsChange = viewModelService.ToggleMergePoint(this, commitViewModel.Commit);

				ScrollRows(rowsChange);
				VirtualItemsSource.DataChanged(width);
				return true;
			}

			return false;
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
			IsShowCommitDetails = !IsShowCommitDetails;
		}


		private void ShowCurrentBranch()
		{
			viewModelService.ShowBranch(this, Repository.CurrentBranch);
		}


		private async void TryUpdateAllBranches()
		{
			Log.Debug("Try update all branches");

			using (busyIndicator.Progress)
			{
				Branch currentBranch = Repository.CurrentBranch;
				Branch uncommittedBranch = UnCommited?.Branch;
				IEnumerable<Branch> updatableBranches = Repository.Branches
					.Where(b =>
					b != currentBranch
					&& b != uncommittedBranch
					&& b.RemoteAheadCount > 0
					&& b.LocalAheadCount == 0).ToList();

				string workingFolder = Repository.MRepository.WorkingFolder;
				foreach (Branch branch in updatableBranches)
				{
					Log.Debug($"Updating branch {branch.Name}");

					await gitService.UpdateBranchAsync(workingFolder, branch.Name);
				}

				if (uncommittedBranch != currentBranch
					&& currentBranch.RemoteAheadCount > 0
					&& currentBranch.LocalAheadCount == 0)
				{
					await gitService.UpdateCurrentBranchAsync(workingFolder);
				}

				await RefreshAfterCommandAsync();
			}
		}

		private bool TryUpdateAllBranchesCanExecute()
		{
			Branch uncommittedBranch = UnCommited?.Branch;

			return Repository.Branches.Any(
				b => b != uncommittedBranch
				&& b.RemoteAheadCount > 0
				&& b.LocalAheadCount == 0);
		}


		private async void PullCurrentBranch()
		{
			using (busyIndicator.Progress)
			{
				string workingFolder = Repository.MRepository.WorkingFolder;
				await gitService.PullCurrentBranchAsync(workingFolder);

				await RefreshAfterCommandAsync();
			}
		}


		private bool PullCurrentBranchCanExecute()
		{
			Branch uncommittedBranch = UnCommited?.Branch;

			return uncommittedBranch != Repository.CurrentBranch
				&& Repository.CurrentBranch.RemoteAheadCount > 0;
		}


		private async void TryPushAllBranches()
		{
			Log.Debug("Try push all branches");

			using (busyIndicator.Progress)
			{
				Branch uncommittedBranch = UnCommited?.Branch;
				IEnumerable<Branch> pushableBranches = Repository.Branches
					.Where(b =>
						b != uncommittedBranch
						&& b.LocalAheadCount > 0
						&& b.RemoteAheadCount == 0).ToList();

				string workingFolder = Repository.MRepository.WorkingFolder;
				foreach (Branch branch in pushableBranches)
				{
					Log.Debug($"Push branch {branch.Name}");

					await gitService.PushBranchAsync(workingFolder, branch.Name);
				}

				await RefreshAfterCommandAsync();
			}
		}


		private bool TryPushAllBranchesCanExecute()
		{
			Branch uncommittedBranch = UnCommited?.Branch;

			return Repository.Branches.Any(
				b => b != uncommittedBranch
				&& b.LocalAheadCount > 0
				&& b.RemoteAheadCount == 0);
		}


		private async void PushCurrentBranch()
		{
			using (busyIndicator.Progress)
			{
				string workingFolder = Repository.MRepository.WorkingFolder;
				await gitService.PushCurrentBranchAsync(workingFolder);

				await RefreshAfterCommandAsync();
			}
		}


		private bool PushCurrentBranchCanExecute()
		{
			Branch uncommittedBranch = UnCommited?.Branch;

			return uncommittedBranch != Repository.CurrentBranch
				&& Repository.CurrentBranch.LocalAheadCount > 0
				&& Repository.CurrentBranch.RemoteAheadCount == 0;
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

			bool isHandled = false;
			if ((absx < 10) && (absy < 10))
			{
				isHandled = Clicked(column, row, isControl);
			}

			if (!isHandled && (absx < 10))
			{				
			}
		}
	}
}