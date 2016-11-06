using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Branching;
using GitMind.Features.Branching.Private;
using GitMind.Features.Committing;
using GitMind.Features.Diffing;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.MainWindowViews;
using GitMind.Utils;
using GitMind.Utils.UI;
using GitMind.Utils.UI.VirtualCanvas;
using Application = System.Windows.Application;
using IBranchService = GitMind.Features.Branching.IBranchService;
using ListBox = System.Windows.Controls.ListBox;


namespace GitMind.RepositoryViews
{
	/// <summary>
	/// View model
	/// </summary>
	[SingleInstance]
	internal class RepositoryViewModel : ViewModel, IRepositoryCommands, IRepositoryMgr
	{
		private static readonly TimeSpan FilterDelay = TimeSpan.FromMilliseconds(300);

		private readonly IViewModelService viewModelService;
		private readonly IRepositoryService repositoryService;
		private readonly IGitCommitsService gitCommitsService;
		private readonly IGitBranchService gitBranchService;
		private readonly IGitInfoService gitInfoService;
		private readonly INetworkService networkService;
		private readonly IBrushService brushService;
		private readonly IDiffService diffService;
		private readonly WorkingFolder workingFolder;
		private readonly WindowOwner owner;
		private readonly IBranchService branchService;
		private readonly ICommandLine commandLine;
		private readonly ICommitService commitService;

		private readonly BusyIndicator busyIndicator;
		private readonly IProgressService progress;


		private readonly DispatcherTimer filterTriggerTimer = new DispatcherTimer();
		private string settingFilterText = "";
		private bool isInternalDialog = false;

		private int width = 0;
		private int graphWidth = 0;

		public List<BranchViewModel> Branches { get; } = new List<BranchViewModel>();
		public List<MergeViewModel> Merges { get; } = new List<MergeViewModel>();
		public List<CommitViewModel> Commits { get; } = new List<CommitViewModel>();


		public Dictionary<string, CommitViewModel> CommitsById { get; } =
			new Dictionary<string, CommitViewModel>();

		private DateTime fetchedTime = DateTime.MinValue;
		private DateTime FreshRepositoryTime = DateTime.MinValue;

		private static readonly TimeSpan ActivateRemoteCheckInterval = TimeSpan.FromSeconds(15);
		private static readonly TimeSpan AutoRemoteCheckInterval = TimeSpan.FromMinutes(9);
		private static readonly TimeSpan FreshRepositoryInterval = TimeSpan.FromMinutes(10);

		private readonly TaskThrottler refreshThrottler = new TaskThrottler(1);


		public IReadOnlyList<Branch> SpecifiedBranches { get; set; } = new Branch[0];

		//public string WorkingFolder { get; set; }

		public IReadOnlyList<BranchName> SpecifiedBranchNames { get; set; }
		public ZoomableCanvas Canvas { get; set; }


		public RepositoryViewModel(
			WorkingFolder workingFolder,
			IDiffService diffService,
			WindowOwner owner,
			IBranchService branchService,
			ICommandLine commandLine,
			IViewModelService viewModelService,
			ICommitService commitService,
			IRepositoryService repositoryService,
			IGitCommitsService gitCommitsService,
			IGitBranchService gitBranchService,
			IGitInfoService gitInfoService,
			INetworkService networkService,
			IBrushService brushService,
			BusyIndicator busyIndicator,
			IProgressService progressService,
			Func<CommitDetailsViewModel> commitDetailsViewModelProvider,
			CommitCommand commitCommand,
			MergeCommand mergeCommand,
			ToggleDetailsCommand toggleDetailsCommand,
			DeleteBranchCommand deleteBranchCommand,
			UncommitCommand uncommitCommand)
		{
			this.workingFolder = workingFolder;
			this.diffService = diffService;
			this.owner = owner;
			this.branchService = branchService;
			this.commandLine = commandLine;
			this.viewModelService = viewModelService;
			this.commitService = commitService;
			this.repositoryService = repositoryService;
			this.gitCommitsService = gitCommitsService;
			this.gitBranchService = gitBranchService;
			this.gitInfoService = gitInfoService;
			this.networkService = networkService;
			this.brushService = brushService;
			this.busyIndicator = busyIndicator;
			this.progress = progressService;

			VirtualItemsSource = new RepositoryVirtualItemsSource(Branches, Merges, Commits);

			filterTriggerTimer.Tick += FilterTrigger;
			filterTriggerTimer.Interval = FilterDelay;

			CommitDetailsViewModel = commitDetailsViewModelProvider();
			CommitCommand = commitCommand;
			MergeBranchCommand = mergeCommand;
			ToggleDetailsCommand = toggleDetailsCommand;
			DeleteBranchCommand = deleteBranchCommand;
			UncommitCommand = uncommitCommand;
		}



		public Repository Repository { get; private set; }

		public Branch MergingBranch { get; private set; }


		public DisabledStatus DisableStatus()
		{
			return new DisabledStatus(this);
		}

		public void SetIsInternalDialog(bool isInternal)
		{
			isInternalDialog = isInternal;
		}


		public Commit UnCommited
		{
			get { return Get<Commit>(); }
			set
			{
				Set(value);
				StatusText = value?.Subject;
				IsUncommitted = value != null && !value.HasConflicts;
				Notify(nameof(StatusText));
			}
		}

		public string StatusText
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string FetchErrorText
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool IsUncommitted
		{
			get { return Get(); }
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

		public string PullCurrentBranchText => $"Update current branch '{CurrentBranchName}'";

		public string PushCurrentBranchText => $"Push current branch '{CurrentBranchName}'";


		public Command<Branch> ShowBranchCommand => Command<Branch>(ShowBranch);
		public Command<Branch> HideBranchCommand => Command<Branch>(HideBranch);
		public Command<Branch> DeleteBranchCommand { get; }

		public Command<Branch> PublishBranchCommand => Command<Branch>(
			branch => branchService.PublishBranch(branch));
		public Command<Branch> PushBranchCommand => Command<Branch>(
			branch => branchService.PushBranch(branch));
		public Command<Branch> UpdateBranchCommand => Command<Branch>(
			branch => branchService.UpdateBranch(branch));
		public Command<Commit> ShowDiffCommand => Command<Commit>(ShowDiff);
		public Command ToggleDetailsCommand { get; }
		public Command ShowUncommittedDetailsCommand => Command(ShowUncommittedDetails);
		public Command ShowCurrentBranchCommand => Command(ShowCurrentBranch);
		public Command<Commit> SetBranchCommand => AsyncCommand<Commit>(SetBranchAsync);


		public Command<Branch> MergeBranchCommand { get; }


		public Command UndoCleanWorkingFolderCommand => AsyncCommand(UndoCleanWorkingFolderAsync);
		public Command UndoUncommittedChangesCommand => AsyncCommand(UndoUncommittedChangesAsync);
		public Command<Commit> UncommitCommand { get; }


		private Command CommitCommand { get; }




		public Command ShowUncommittedDiffCommand => Command(
			() => commitService.ShowUncommittedDiff(),
			() => IsUncommitted);

		public Command ShowSelectedDiffCommand => Command(ShowSelectedDiff);

		public Command TryUpdateAllBranchesCommand => Command(
			TryUpdateAllBranches, CanExecuteTryUpdateAllBranches);

		public Command PullCurrentBranchCommand => Command(
			PullCurrentBranch, CanExecutePullCurrentBranch);

		public Command TryPushAllBranchesCommand => Command(
			TryPushAllBranches, CanExecuteTryPushAllBranches);

		public Command PushCurrentBranchCommand => Command(
			PushCurrentBranch, CanExecutePushCurrentBranch);



		public RepositoryVirtualItemsSource VirtualItemsSource { get; }

		public ObservableCollection<BranchItem> ShowableBranches { get; }
			= new ObservableCollection<BranchItem>();

		public ObservableCollection<BranchItem> DeletableBranches { get; }
			= new ObservableCollection<BranchItem>();

		public ObservableCollection<BranchItem> HidableBranches { get; }
			= new ObservableCollection<BranchItem>();

		public ObservableCollection<BranchItem> ShownBranches { get; }
			= new ObservableCollection<BranchItem>();


		public CommitDetailsViewModel CommitDetailsViewModel { get; }

		public string FilterText { get; private set; } = "";

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


		public void ShowBranch(BranchName branchName)
		{
			SpecifiedBranchNames = new[] { branchName };
		}


		public async Task FirstLoadAsync()
		{
			Repository repository;

			await refreshThrottler.Run(async () =>
			{
				Log.Debug("Loading repository ...");
				bool isRepositoryCached = repositoryService.IsRepositoryCached(workingFolder);
				string statusText = isRepositoryCached ? "Loading ..." : "First time branch structure analyze ...";

				progress.Show(statusText, async () =>
				{
					repository = await repositoryService.GetCachedOrFreshRepositoryAsync(workingFolder);
					UpdateInitialViewModel(repository);
				});


				if (!gitInfoService.IsSupportedRemoteUrl(workingFolder))
				{
					Message.ShowWarning(owner,
						"SSH URL protocol is not yet supported for remote access.\n" +
						"Use git:// or https:// instead.");
				}

				using (busyIndicator.Progress())
				{
					repository = await GetLocalChangesAsync(Repository);
					UpdateViewModel(repository);

					if (commandLine.IsCommit)
					{
						if (CommitCommand.CanExecute())
						{
							CommitCommand.Execute();
						}
					}

					await FetchRemoteChangesAsync(Repository, true);
					repository = await GetLocalChangesAsync(Repository);
					UpdateViewModel(repository);
					Log.Debug("Loaded repository done");
				}

				Log.Debug("Get fresh repository from scratch");
				FreshRepositoryTime = DateTime.Now;
				repository = await repositoryService.GetFreshRepositoryAsync(workingFolder);
				UpdateViewModel(repository);
			});
		}


		public async Task ActivateRefreshAsync()
		{
			if (DateTime.Now - fetchedTime > ActivateRemoteCheckInterval)
			{
				Log.Usage("Activate window");
				using (busyIndicator.Progress())
				{
					await FetchRemoteChangesAsync(Repository, false);
				}
			}
		}


		public async Task AutoRemoteCheckAsync()
		{
			if (DateTime.Now - fetchedTime > AutoRemoteCheckInterval)
			{
				Log.Usage("Automatic remote check");
				await FetchRemoteChangesAsync(Repository, false);
			}
		}


		public Task StatusChangeRefreshAsync(DateTime triggerTime, bool isRepoChange)
		{
			if (isInternalDialog)
			{
				return Task.CompletedTask;
			}

			return refreshThrottler.Run(async () =>
			{
				//if (isRepoChange)
				//{
				//	Log.Debug("Check if Repository has changed");
				//	if (!await repositoryService.IsRepositoryChangedAsync(Repository))
				//	{
				//		Log.Debug("Repository has not changed");
				//		return;
				//	}
				//}

				Log.Debug("Refreshing after status/repo change ...");
				Log.Usage("Refresh after status/repo change");

				using (busyIndicator.Progress())
				{
					Repository repository;
					if (isRepoChange && triggerTime - FreshRepositoryTime > FreshRepositoryInterval)
					{
						Log.Debug("Get fresh repository from scratch");
						FreshRepositoryTime = DateTime.Now;
						repository = await repositoryService.GetFreshRepositoryAsync(workingFolder);
						FreshRepositoryTime = DateTime.Now;
					}
					else
					{
						repository = await GetLocalChangesAsync(Repository);
					}

					UpdateViewModel(repository);
					Log.Debug("Refreshed after status/repo change done");
				}
			});
		}


		public async Task RefreshAfterCommandAsync(bool useFreshRepository)
		{
			isInternalDialog = true;
			await refreshThrottler.Run(async () =>
			{
				Log.Debug("Refreshing after command ...");

				await FetchRemoteChangesAsync(Repository, true);

				Repository repository;
				if (useFreshRepository)
				{
					Log.Debug("Getting fresh repository");
					repository = await repositoryService.GetFreshRepositoryAsync(workingFolder);
				}
				else
				{
					repository = await GetLocalChangesAsync(Repository);
				}

				UpdateViewModel(repository);
				Log.Debug("Refreshed after command done");
			});

			isInternalDialog = false;
		}


		public void SetCurrentMerging(Branch branch)
		{
			MergingBranch = branch;
		}


		public Task ManualRefreshAsync()
		{
			progress.Show("Analyze branch structure", async () =>
			{
				await refreshThrottler.Run(async () =>
				{
					Log.Debug("Refreshing after manual trigger ...");

					await FetchRemoteChangesAsync(Repository, true);

					Log.Debug("Get fresh repository from scratch");
					Repository repository = await repositoryService.GetFreshRepositoryAsync(workingFolder);

					FreshRepositoryTime = DateTime.Now;
					UpdateViewModel(repository);
					Log.Debug("Refreshed after manual trigger done");
				});
			});

			return Task.CompletedTask;
		}


		public void MouseEnterBranch(BranchViewModel branch)
		{
			branch.SetHighlighted();

			if (branch.Branch.IsLocalPart)
			{
				// Local part branch, then do not dim common commits in main branch part
				foreach (CommitViewModel commit in Commits)
				{
					if (commit.Commit.Branch.Id != branch.Branch.Id
						&& !(commit.Commit.IsCommon
							&& commit.Commit.Branch.IsMainPart
							&& commit.Commit.Branch.LocalSubBranch == branch.Branch))
					{
						commit.SetDim();
					}
				}

			}
			else
			{
				// Normal branches and main branches
				foreach (CommitViewModel commit in Commits)
				{
					if (commit.Commit.Branch.Id != branch.Branch.Id)
					{
						commit.SetDim();
					}
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


		private async Task FetchRemoteChangesAsync(Repository repository, bool isFetchNotes)
		{
			Log.Debug("Fetching");
			R result = await networkService.FetchAsync();
			FetchErrorText = "";
			if (result.IsFaulted)
			{
				string message = $"Fetch error: {result.Error.Exception.Message}";
				Log.Warn(message);
				FetchErrorText = message;
			}
			else if (isFetchNotes)
			{
				await networkService.FetchAllNotesAsync();
			}

			fetchedTime = DateTime.Now;
		}


		private void UpdateViewModel(Repository repository)
		{
			Timing t = new Timing();
			Repository = repository;
			if (string.IsNullOrEmpty(FilterText) && string.IsNullOrEmpty(settingFilterText))
			{
				viewModelService.UpdateViewModel(this);

				UpdateViewModel();

				t.Log("Updated repository view model");
			}
		}


		private void UpdateInitialViewModel(Repository repository)
		{
			Timing t = new Timing();
			Repository = repository;
			viewModelService.UpdateViewModel(this);

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
				.Where(b => b.IsLocal && (b.IsRemote || b.IsLocalPart) && b.LocalAheadCount > 0).ToList();

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
				? $"Conflicts in {Repository.Status.ConflictCount} files\""
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
				Set(value);
				CommitViewModel commit = value as CommitViewModel;
				if (commit != null)
				{
					SetCommitsDetails(commit);
				}
			}
		}

		public ListBox ListBox { get; set; }
		public IReadOnlyList<Branch> PreFilterBranches { get; set; }
		public CommitViewModel PreFilterSelectedItem { get; set; }


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

			using (busyIndicator.Progress())
			{
				await viewModelService.SetFilterAsync(this, filterText);
			}

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
					Log.Debug("Scroll to 0 since first position was 0");
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


		public void ScrollRows(int rows)
		{
			int offsetY = Converters.ToY(rows);
			Canvas.Offset = new Point(Canvas.Offset.X, Math.Max(Canvas.Offset.Y - offsetY, 0));
		}


		private void ScrollTo(int rows)
		{
			int offsetY = Converters.ToY(rows);
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

		private void ShowUncommittedDetails()
		{
			SelectedIndex = 0;
			ScrollTo(0);
			IsShowCommitDetails = true;
		}

		private void ShowCurrentBranch()
		{
			viewModelService.ShowBranch(this, Repository.CurrentBranch);
		}


		private void TryUpdateAllBranches()
		{
			Log.Debug("Try update all branches");
			isInternalDialog = true;
			progress.Show("Update all branches ...", async state =>
			{
				Branch currentBranch = Repository.CurrentBranch;
				Branch uncommittedBranch = UnCommited?.Branch;

				R result = await networkService.FetchAsync();

				if (result.IsOk && currentBranch.CanBeUpdated)
				{
					state.SetText($"Update current branch {currentBranch.Name} ...");
					result = await gitBranchService.MergeCurrentBranchAsync();
				}

				if (result.IsFaulted)
				{
					Message.ShowWarning(
						owner,
						$"Failed to update current branch {currentBranch.Name}\n{result.Error.Exception.Message}.");
				}

				IEnumerable<Branch> updatableBranches = Repository.Branches
					.Where(b =>
						!b.IsCurrentBranch
						&& b != uncommittedBranch
						&& b.RemoteAheadCount > 0
						&& b.LocalAheadCount == 0).ToList();

				foreach (Branch branch in updatableBranches)
				{
					state.SetText($"Update branch {branch.Name} ...");

					await networkService.FetchBranchAsync(branch.Name);
				}

				state.SetText("Update all branches ...");
				await networkService.FetchAllNotesAsync();

				state.SetText($"Update status after update all branches ...");
				await RefreshAfterCommandAsync(false);
			});
		}

		private bool CanExecuteTryUpdateAllBranches()
		{
			//return false;
			if (!string.IsNullOrEmpty(ConflictsText))
			{
				return false;
			}

			Branch uncommittedBranch = UnCommited?.Branch;

			return Repository.Branches.Any(
				b => b != uncommittedBranch
				&& b.RemoteAheadCount > 0
				&& b.LocalAheadCount == 0);
		}


		private void PullCurrentBranch()
		{
			isInternalDialog = true;
			BranchName branchName = Repository.CurrentBranch.Name;
			progress.Show($"Update current branch {branchName} ...", async state =>
			{
				R result = await networkService.FetchAsync();
				if (result.IsOk)
				{
					result = await gitBranchService.MergeCurrentBranchAsync();

					await networkService.FetchAllNotesAsync();
				}

				if (result.IsFaulted)
				{
					Message.ShowWarning(
						owner, $"Failed to update current branch {branchName}.\n{result.Error.Exception.Message}");
				}

				state.SetText($"Update status after pull current branch {branchName} ...");
				await RefreshAfterCommandAsync(false);
			});
		}


		private bool CanExecutePullCurrentBranch()
		{
			return Repository.CurrentBranch.CanBeUpdated;
		}


		private void TryPushAllBranches()
		{
			Log.Debug("Try push all branches");
			isInternalDialog = true;
			progress.Show("Push all branches ...", async state =>
			{
				Branch currentBranch = Repository.CurrentBranch;

				await networkService.PushNotesAsync( Repository.RootId);

				R result = R.Ok;
				if (currentBranch.CanBePushed)
				{
					state.SetText($"Push current branch {currentBranch.Name} ...");
					result = await networkService.PushCurrentBranchAsync();
				}

				if (result.IsFaulted)
				{
					Message.ShowWarning(
						owner,
						$"Failed to push current branch {currentBranch.Name}.\n{result.Error.Exception.Message}");
				}

				IEnumerable<Branch> pushableBranches = Repository.Branches
					.Where(b => !b.IsCurrentBranch && b.CanBePushed)
					.ToList();

				foreach (Branch branch in pushableBranches)
				{
					state.SetText($"Push branch {branch.Name} ...");

					await networkService.PushBranchAsync(branch.Name);
				}

				state.SetText("Update status after push all branches ...");
				await RefreshAfterCommandAsync(true);
			});
		}


		private bool CanExecuteTryPushAllBranches()
		{
			return Repository.Branches.Any(b => b.CanBePushed);
		}


		private void PushCurrentBranch()
		{
			isInternalDialog = true;
			BranchName branchName = Repository.CurrentBranch.Name;
			progress.Show($"Push current branch {branchName} ...", async state =>
			{
				await networkService.PushNotesAsync(Repository.RootId);

				R result = await networkService.PushCurrentBranchAsync();

				if (result.IsFaulted)
				{
					Message.ShowWarning(
						owner, $"Failed to push current branch {branchName}.\n{result.Error.Exception.Message}");
				}

				state.SetText($"Updating status after push {branchName} ...");
				await RefreshAfterCommandAsync(true);
			});
		}


		private bool CanExecutePushCurrentBranch()
		{
			return Repository.CurrentBranch.CanBePushed;
		}


		//private async Task UncommitAsync(Commit commit)
		//{
		//	await commitService.UnCommitAsync(commit);
		//}


		private void ShowDiff(Commit commit)
		{
			if (ListBox.SelectedItems.Count < 2)
			{
				diffService.ShowDiffAsync(commit.Id).RunInBackground();
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

					diffService.ShowDiffRangeAsync(id1, id2).RunInBackground();
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
					diffService.ShowDiffRangeAsync(id1, id2).RunInBackground(); ;
				}
			}
		}


		private Task SetBranchAsync(Commit commit)
		{
			SetBranchPromptDialog dialog = new SetBranchPromptDialog();
			dialog.PromptText = commit.SpecifiedBranchName;
			dialog.IsAutomatically = commit.SpecifiedBranchName == null;
			foreach (Branch childBranch in commit.Branch.GetChildBranches())
			{
				if (!childBranch.IsMultiBranch && !childBranch.Name.StartsWith("_"))
				{
					dialog.AddBranchName(childBranch.Name);
				}
			}

			isInternalDialog = true;
			if (dialog.ShowDialog() == true)
			{
				Application.Current.MainWindow.Focus();
				BranchName branchName = dialog.IsAutomatically ? null : dialog.PromptText?.Trim();

				if (commit.SpecifiedBranchName != branchName)
				{
					progress.Show($"Set commit branch name {branchName} ...", async () =>
					{
						await repositoryService.SetSpecifiedCommitBranchAsync(
							workingFolder, commit.Id, commit.Repository.RootId, branchName);
						if (branchName != null)
						{
							SpecifiedBranchNames = new[] { branchName };
						}

						await RefreshAfterCommandAsync(true);
					});
				}
			}
			else
			{
				Application.Current.MainWindow.Focus();
			}

			isInternalDialog = false;
			return Task.CompletedTask;
		}



		private async void ShowSelectedDiff()
		{
			CommitViewModel commit = SelectedItem as CommitViewModel;

			if (commit != null)
			{
				await diffService.ShowDiffAsync(commit.Commit.Id);
			}
		}


		private async Task UndoCleanWorkingFolderAsync()
		{
			R<IReadOnlyList<string>> failedPaths = R.From(new string[0].AsReadOnlyList());
			await Task.Yield();

			isInternalDialog = true;
			progress.Show($"Undo changes and clean working folder {workingFolder} ...", async () =>
			{
				failedPaths = await gitCommitsService.UndoCleanWorkingFolderAsync();

				await RefreshAfterCommandAsync(false);
			});

			if (failedPaths.IsFaulted)
			{
				Message.ShowWarning(owner, failedPaths.ToString());
			}
			else if (failedPaths.Value.Any())
			{
				string text = $"Failed to undo and clean working folder.\nSome items where locked:\n";
				foreach (string path in failedPaths.Value.Take(10))
				{
					text += $"\n   {path}";
				}
				if (failedPaths.Value.Count > 10)
				{
					text += "   ...";
				}

				Message.ShowWarning(owner, text);
			}

			isInternalDialog = true;
		}


		private async Task UndoUncommittedChangesAsync()
		{
			await Task.Yield();

			isInternalDialog = true;
			progress.Show($"Undo changes in working folder {workingFolder} ...", async () =>
			{
				await gitCommitsService.UndoWorkingFolderAsync();

				await RefreshAfterCommandAsync(false);
			});

			isInternalDialog = true;
		}


		public void Clicked(Point position)
		{
			double clickX = position.X - 9;
			double clickY = position.Y - 5;

			int row = Converters.ToRow(clickY);

			if (row < 0 || row >= Commits.Count - 1 || clickX < 0 || clickX >= graphWidth)
			{
				// Click is not within supported area.
				return;
			}

			CommitViewModel commitViewModel = Commits[row];
			int xDotCenter = commitViewModel.X;
			int yDotCenter = commitViewModel.Y;

			double absx = Math.Abs(xDotCenter - clickX);
			double absy = Math.Abs(yDotCenter - clickY);

			if ((absx < 10) && (absy < 10))
			{
				Clicked(commitViewModel);
			}
		}

		private void Clicked(CommitViewModel commitViewModel)
		{
			if (commitViewModel.IsMergePoint)
			{
				// User clicked on a merge point (toggle between expanded and collapsed)
				int rowsChange = viewModelService.ToggleMergePoint(this, commitViewModel.Commit);

				ScrollRows(rowsChange);
				VirtualItemsSource.DataChanged(width);
			}
		}


		public void SetMainWindowFocus()
		{
			owner.Window.Focus();
		}
	}
}