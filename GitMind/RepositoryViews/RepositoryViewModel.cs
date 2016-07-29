using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
		private readonly IDiffService diffService = new DiffService();

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
			BusyIndicator busyIndicator, Command refreshManuallyCommand)
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

			CommitDetailsViewModel = new CommitDetailsViewModel(UndoUncommittedFileCommand);
		}


		public Repository Repository { get; private set; }




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

		public string ConflictsText
		{
			get { return Get(); }
			set { Set(value); }
		}


		public string CurrentBranchName
		{
			get { return Get(); }
			set { Set(value).Notify(nameof(PullCurrentBranchText), nameof(PushCurrentBranchText)); }
		}

		public Brush CurrentBranchBrush
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string PullCurrentBranchText => $"Pull current branch '{CurrentBranchName}'";

		public string PushCurrentBranchText => $"Push current branch '{CurrentBranchName}'";


		public Command<Branch> ShowBranchCommand => Command<Branch>(ShowBranch);
		public Command<Branch> HideBranchCommand => Command<Branch>(HideBranch);
		public Command<Commit> ShowDiffCommand => Command<Commit>(ShowDiff);
		public Command ToggleDetailsCommand => Command(ToggleDetails);
		public Command ShowCurrentBranchCommand => Command(ShowCurrentBranch);
		public Command<Commit> SetBranchCommand => AsyncCommand<Commit>(SetBranchAsync);
		public Command<Branch> SwitchBranchCommand => AsyncCommand<Branch>(SwitchBranchAsync, CanExecuteSwitchBranch);
		public Command<string> UndoUncommittedFileCommand => AsyncCommand<string>(UndoUncommittedFileAsync);
		public Command<Branch> MergeBranchCommand => AsyncCommand<Branch>(MergeBranchAsync);
		public Command<Commit> SwitchToCommitCommand => AsyncCommand<Commit>(SwitchToCommitAsync, CanExecuteSwitchToCommit);


		public Command TryUpdateAllBranchesCommand => Command(
			TryUpdateAllBranches, CanExecuteTryUpdateAllBranches);

		public Command PullCurrentBranchCommand => Command(
			PullCurrentBranch, CanExecutePullCurrentBranch);

		public Command TryPushAllBranchesCommand => Command(
			TryPushAllBranches, CanExecuteTryPushAllBranches);

		public Command PushCurrentBranchCommand => Command(
			PushCurrentBranch, CanExecutePushCurrentBranch);



		public RepositoryVirtualItemsSource VirtualItemsSource { get; }

		public IReadOnlyList<BranchItem> ShowableBranches => BranchItem.GetBranches(
			Repository.Branches
			.Where(b => b.IsActive && b.Name != "master")
			.Where(b => !HidableBranches.Any(ab => ab.Branch.Id == b.Id)),
			ShowBranchCommand,
			MergeBranchCommand);

		public ObservableCollection<BranchItem> HidableBranches { get; }
			= new ObservableCollection<BranchItem>();

		public ObservableCollection<BranchItem> ShownBranches { get; }
			= new ObservableCollection<BranchItem>();


		public CommitDetailsViewModel CommitDetailsViewModel { get; } 

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


		public Task RefreshAfterCommandAsync()
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
			NotifyAll();

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

			ConflictsText = Repository.Status.ConflictCount > 0
				? $"Conflicts in {Repository.Status.ConflictCount} files\n\n" +
				"User other tool to resolve conflicts for now."
				: null;
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

		public ListBox ListBox { get; set; }


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
			TrySetSelectedCommitPosition(commitPosition, true);
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


		private void TrySetSelectedCommitPosition(CommitPosition commitPosition, bool ignoreTopIndex = false)
		{
			if (commitPosition != null)
			{
				if (!ignoreTopIndex && commitPosition.Index == 0)
				{
					// The index was 0 (top) lest ensure the index remains 0 again
					ScrollTo(0);
					if (Commits.Any())
					{
						SelectedIndex = 0;
						SelectedItem = Commits.First();
					}

					return;
				}

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
				// Click is not within supported area.
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
				string workingFolder = Repository.MRepository.WorkingFolder;

				await gitService.FetchAsync(workingFolder);

				Branch currentBranch = Repository.CurrentBranch;
				Branch uncommittedBranch = UnCommited?.Branch;
				IEnumerable<Branch> updatableBranches = Repository.Branches
				 .Where(b =>
				 b != currentBranch
				 && b != uncommittedBranch
				 && b.RemoteAheadCount > 0
				 && b.LocalAheadCount == 0).ToList();


				foreach (Branch branch in updatableBranches)
				{
					Log.Debug($"Updating branch {branch.Name}");

					await gitService.FetchBranchAsync(workingFolder, branch.Name);
				}

				if (uncommittedBranch != currentBranch
					&& currentBranch.RemoteAheadCount > 0
					&& currentBranch.LocalAheadCount == 0)
				{
					Log.Debug($"Updating current branch {currentBranch.Name}");
					await gitService.MergeCurrentBranchFastForwardOnlyAsync(workingFolder);
				}

				await RefreshAfterCommandAsync();
			}
		}

		private bool CanExecuteTryUpdateAllBranches()
		{
			return false;
			//if (!string.IsNullOrEmpty(ConflictsText))
			//{
			//	return false;
			//}

			//Branch uncommittedBranch = UnCommited?.Branch;

			//return Repository.Branches.Any(
			//	b => b != uncommittedBranch
			//	&& b.RemoteAheadCount > 0
			//	&& b.LocalAheadCount == 0);
		}


		private async void PullCurrentBranch()
		{
			using (busyIndicator.Progress)
			{
				string workingFolder = Repository.MRepository.WorkingFolder;

				await gitService.FetchAsync(workingFolder);

				await gitService.MergeCurrentBranchAsync(workingFolder);

				await RefreshAfterCommandAsync();
			}
		}


		private bool CanExecutePullCurrentBranch()
		{
			if (!string.IsNullOrEmpty(ConflictsText))
			{
				return false;
			}

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


		private bool CanExecuteTryPushAllBranches()
		{
			return false;
			//if (!string.IsNullOrEmpty(ConflictsText))
			//{
			//	return false;
			//}

			//Branch uncommittedBranch = UnCommited?.Branch;

			//return Repository.Branches.Any(
			//	b => b != uncommittedBranch
			//	&& b.LocalAheadCount > 0
			//	&& b.RemoteAheadCount == 0);
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


		private bool CanExecutePushCurrentBranch()
		{
			if (!string.IsNullOrEmpty(ConflictsText))
			{
				return false;
			}

			Branch uncommittedBranch = UnCommited?.Branch;

			return uncommittedBranch != Repository.CurrentBranch
				&& Repository.CurrentBranch.LocalAheadCount > 0
				&& Repository.CurrentBranch.RemoteAheadCount == 0;
		}



		private void ShowDiff(Commit commit)
		{
			if (ListBox.SelectedItems.Count < 2)
			{
				diffService.ShowDiffAsync(commit.Id, WorkingFolder).RunInBackground();
			}
			else
			{
				CommitViewModel topCommit = ListBox.SelectedItems[0] as CommitViewModel;
				int bottomIndex = ListBox.SelectedItems.Count - 1;
				CommitViewModel bottomCommit = ListBox.SelectedItems[bottomIndex] as CommitViewModel;

				if (topCommit != null && bottomCommit != null)
				{
					// Selection was made with ctrl-click. Lets take top and bottom commits as range
					// even if there are more commits in the middle
					string id1 = topCommit.Commit.Id;
					string id2 = bottomCommit.Commit.HasFirstParent
						? bottomCommit.Commit.FirstParent.Id
						: bottomCommit.Commit.Id;

					diffService.ShowDiffRangeAsync(id1, id2, WorkingFolder).RunInBackground();
				}
				else if (topCommit != null)
				{
					// Selection was probably done with shift-click. Fore some reason SelectedItems
					// only contains first selected item, other items are null, but there are one null
					// item for each selected item plus one extra.
					// Find the range by iterating first parents of the top commit (selected items count)
					Commit current = topCommit.Commit;
					for (int i = 0; i < bottomIndex; i++)
					{
						if (!current.HasFirstParent)
						{
							break;
						}
						current = current.FirstParent;
					}

					string id1 = topCommit.Commit.Id;
					string id2 = current.Id;
					diffService.ShowDiffRangeAsync(id1, id2, WorkingFolder).RunInBackground(); ;
				}
			}
		}


		private async Task SetBranchAsync(Commit commit)
		{
			SetBranchPromptDialog dialog = new SetBranchPromptDialog();
			dialog.PromptText = commit.SpecifiedBranchName;
			dialog.IsAutomatically = string.IsNullOrEmpty(commit.SpecifiedBranchName);
			foreach (Branch childBranch in commit.Branch.GetChildBranches())
			{
				if (!childBranch.IsMultiBranch && !childBranch.Name.StartsWith("_"))
				{
					dialog.AddBranchName(childBranch.Name);
				}
			}

			if (dialog.ShowDialog() == true)
			{
				Application.Current.MainWindow.Focus();
				string branchName = dialog.IsAutomatically ? null : dialog.PromptText?.Trim();
				string workingFolder = WorkingFolder;

				if (commit.SpecifiedBranchName != branchName)
				{
					await repositoryService.SetSpecifiedCommitBranchAsync(commit.Id, branchName, workingFolder);

					await RefreshAfterCommandAsync();
				}
			}
			else
			{
				Application.Current.MainWindow.Focus();
			}
		}


		private async Task SwitchBranchAsync(Branch branch)
		{
			using (busyIndicator.Progress)
			{
				await gitService.SwitchToBranchAsync(WorkingFolder, branch.Name);

				await RefreshAfterCommandAsync();
			}		
		}


		private bool CanExecuteSwitchBranch(Branch branch)
		{
			return
				Repository.Status.ConflictCount == 0
				&& Repository.CurrentBranch.Id != branch.Id;
		}



		private async Task UndoUncommittedFileAsync(string path)
		{
			using (busyIndicator.Progress)
			{
				await gitService.UndoFileInCurrentBranchAsync(WorkingFolder, path);

				await RefreshAfterCommandAsync();
			}
		}


		private async Task MergeBranchAsync(Branch branch)
		{
			using (busyIndicator.Progress)
			{
				await gitService.MergeAsync(WorkingFolder, branch.Name);

				await RefreshAfterCommandAsync();
			}
		}



		private async Task SwitchToCommitAsync(Commit commit)
		{
			using (busyIndicator.Progress)
			{
				string proposedNamed = commit == commit.Branch.TipCommit
					? commit.Branch.Name
					: $"_tmp_{commit.Branch.Name}";

			await gitService.SwitchToCommitAsync(WorkingFolder, commit.Id, proposedNamed);

				await RefreshAfterCommandAsync();
			}
		}


		private bool CanExecuteSwitchToCommit(Commit commit)
		{
			return Repository.Status.StatusCount == 0 && Repository.Status.ConflictCount == 0;
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