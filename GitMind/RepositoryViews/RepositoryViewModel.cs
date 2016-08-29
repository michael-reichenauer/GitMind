using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Committing;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.UI;
using GitMind.Utils.UI.VirtualCanvas;
using Application = System.Windows.Application;
using BranchService = GitMind.Features.Branching.BranchService;
using IBranchService = GitMind.Features.Branching.IBranchService;
using ListBox = System.Windows.Controls.ListBox;


namespace GitMind.RepositoryViews
{
	/// <summary>
	/// View model
	/// </summary>
	internal class RepositoryViewModel : ViewModel, IRepositoryCommands
	{
		private static readonly TimeSpan FilterDelay = TimeSpan.FromMilliseconds(300);

		private readonly IViewModelService viewModelService;
		private readonly IRepositoryService repositoryService = new RepositoryService();
		private readonly IGitService gitService = new GitService();
		private readonly IBrushService brushService = new BrushService();
		private readonly IDiffService diffService = new DiffService();
		private readonly IBranchService branchService = new BranchService();
		private readonly ICommitService commitService = new CommitService();

		private readonly BusyIndicator busyIndicator;


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




		public RepositoryViewModel(Window owner, BusyIndicator busyIndicator)
			: this(owner, new ViewModelService(), busyIndicator)
		{
		}


		public IReadOnlyList<Branch> SpecifiedBranches { get; set; } = new Branch[0];
		public string WorkingFolder { get; set; }
		public IReadOnlyList<string> SpecifiedBranchNames { get; set; }
		public ZoomableCanvas Canvas { get; set; }


		public RepositoryViewModel(
			Window owner,
			IViewModelService viewModelService,
			BusyIndicator busyIndicator)
		{
			this.Owner = owner;
			this.viewModelService = viewModelService;
			this.busyIndicator = busyIndicator;


			VirtualItemsSource = new RepositoryVirtualItemsSource(Branches, Merges, Commits);

			filterTriggerTimer.Tick += FilterTrigger;
			filterTriggerTimer.Interval = FilterDelay;

			CommitDetailsViewModel = new CommitDetailsViewModel(this);
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
		public Command<Branch> DeleteLocalBranchCommand => Command<Branch>(
			branch => branchService.DeleteLocalBranch(this, branch));
		public Command<Branch> DeleteRemoteBranchCommand => Command<Branch>(
			branch => branchService.DeleteRemoteBranch(this, branch));
		public Command<Commit> ShowDiffCommand => Command<Commit>(ShowDiff);
		public Command ToggleDetailsCommand => Command(ToggleDetails);
		public Command ShowUncommittedDetailsCommand => Command(ShowUncommittedDetails);
		public Command ShowCurrentBranchCommand => Command(ShowCurrentBranch);
		public Command<Commit> SetBranchCommand => AsyncCommand<Commit>(SetBranchAsync);


		public Command<Branch> MergeBranchCommand => AsyncCommand<Branch>(
			branch => branchService.MergeBranchAsync(this, branch));


		public Command UndoCleanWorkingFolderCommand => AsyncCommand(UndoCleanWorkingFolderAsync);
		public Command UndoUncommittedChangesCommand => AsyncCommand(UndoUncommittedChangesAsync);
		public Command CommitCommand => AsyncCommand(
			() => commitService.CommitChangesAsync(this),
			() => IsUncommitted);

		public Command ShowUncommittedDiffCommand => Command(
			() => commitService.ShowUncommittedDiff(this),
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

		public ObservableCollection<BranchItem> DeletableLocalBranches { get; }
			= new ObservableCollection<BranchItem>();

		public ObservableCollection<BranchItem> DeletableRemoteBranches { get; }
			= new ObservableCollection<BranchItem>();

		public ObservableCollection<BranchItem> HidableBranches { get; }
			= new ObservableCollection<BranchItem>();

		public ObservableCollection<BranchItem> ShownBranches { get; }
			= new ObservableCollection<BranchItem>();


		public CommitDetailsViewModel CommitDetailsViewModel { get; }

		public string FilterText { get; private set; } = "";
		//	public string FilteredText { get; private set; } = "";

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


		public void AddSpecifiedBranch(string branchName)
		{
			SpecifiedBranchNames = new[] { branchName };
		}


		public Task FirstLoadAsync()
		{
			Repository repository;
			return refreshThrottler.Run(async () =>
			{
				Log.Debug("Loading repository ...");
				bool isRepositoryCached = repositoryService.IsRepositoryCached(WorkingFolder);
				string statusText = isRepositoryCached ? "Loading ..." : "First time, building new model ...";

				Progress.ShowDialog(Owner, statusText, async () =>
				{
					repository = await repositoryService.GetCachedOrFreshRepositoryAsync(WorkingFolder);
					UpdateInitialViewModel(repository);
				});

				using (busyIndicator.Progress())
				{
					repository = await GetLocalChangesAsync(Repository);
					UpdateViewModel(repository);

					await FetchRemoteChangesAsync(Repository, true);
					repository = await GetLocalChangesAsync(Repository);
					UpdateViewModel(repository);
					Log.Debug("Loaded repository done");
				}

				Log.Debug("Get fresh repository from scratch");
				FreshRepositoryTime = DateTime.Now;
				repository = await repositoryService.GetFreshRepositoryAsync(WorkingFolder);
				UpdateViewModel(repository);
			});
		}


		public async Task ActivateRefreshAsync()
		{
			if (DateTime.Now - fetchedTime > ActivateRemoteCheckInterval)
			{
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
				Log.Debug("Refreshing after status/repo change ...");

				using (busyIndicator.Progress())
				{
					Repository repository;
					if (isRepoChange && triggerTime - FreshRepositoryTime > FreshRepositoryInterval)
					{
						Log.Debug("Get fresh repository from scratch");
						FreshRepositoryTime = DateTime.Now;
						repository = await repositoryService.GetFreshRepositoryAsync(WorkingFolder);
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
					repository = await repositoryService.GetFreshRepositoryAsync(WorkingFolder);
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
			Progress.ShowDialog(Owner, "Rebuilding new model", async () =>
			{
				await refreshThrottler.Run(async () =>
				{
					Log.Debug("Refreshing after manual trigger ...");

					await FetchRemoteChangesAsync(Repository, true);

					Log.Debug("Get fresh repository from scratch");
					Repository repository = await repositoryService.GetFreshRepositoryAsync(WorkingFolder);

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


		private async Task FetchRemoteChangesAsync(Repository repository, bool isFetchNotes)
		{
			Log.Debug("Fetch");
			await gitService.FetchAsync(repository.MRepository.WorkingFolder);
			if (isFetchNotes)
			{
				await gitService.FetchNotesAsync(repository.MRepository.WorkingFolder);
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
		public Window Owner { get; }


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
					Log.Warn("Scroll to 0 since first position was 0");
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

			Log.Warn("Scroll to 0");
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
			Progress.ShowDialog(Owner, "Update all branches ...", async progress =>
			{
				string workingFolder = Repository.MRepository.WorkingFolder;
				Branch currentBranch = Repository.CurrentBranch;
				Branch uncommittedBranch = UnCommited?.Branch;

				await gitService.FetchAsync(workingFolder);

				if (uncommittedBranch != currentBranch
						&& currentBranch.RemoteAheadCount > 0
						&& currentBranch.LocalAheadCount == 0)
				{
					progress.SetText($"Update current branch {currentBranch.Name} ...");
					await gitService.MergeCurrentBranchFastForwardOnlyAsync(workingFolder);
				}

				IEnumerable<Branch> updatableBranches = Repository.Branches
					.Where(b =>
						b != currentBranch
						&& b != uncommittedBranch
						&& b.RemoteAheadCount > 0
						&& b.LocalAheadCount == 0).ToList();

				foreach (Branch branch in updatableBranches)
				{
					progress.SetText($"Update branch {branch.Name} ...");

					await gitService.FetchBranchAsync(workingFolder, branch.Name);
				}

				progress.SetText("Update all branches ...");
				await gitService.FetchNotesAsync(workingFolder);

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
			string branchName = Repository.CurrentBranch.Name;
			Progress.ShowDialog(Owner, $"Update current branch {branchName} ...", async () =>
			{
				string workingFolder = Repository.MRepository.WorkingFolder;

				await gitService.FetchAsync(workingFolder);
				await gitService.MergeCurrentBranchAsync(workingFolder);

				await gitService.FetchNotesAsync(workingFolder);
				await RefreshAfterCommandAsync(false);
			});
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


		private void TryPushAllBranches()
		{
			Log.Debug("Try push all branches");
			isInternalDialog = true;
			Progress.ShowDialog(Owner, "Push all branches ...", async progress =>
			{
				string workingFolder = Repository.MRepository.WorkingFolder;
				Branch currentBranch = Repository.CurrentBranch;
				Branch uncommittedBranch = UnCommited?.Branch;

				await gitService.PushNotesAsync(workingFolder, Repository.RootId);

				if (uncommittedBranch != currentBranch
						&& currentBranch.LocalAheadCount > 0
						&& currentBranch.RemoteAheadCount == 0)
				{
					progress.SetText($"Push current branch {currentBranch.Name} ...");
					CredentialHandler credentialHandler = new CredentialHandler(Owner);
					await gitService.PushCurrentBranchAsync(workingFolder, credentialHandler);
				}

				IEnumerable<Branch> pushableBranches = Repository.Branches
					.Where(b =>
						b != currentBranch
						&& b != uncommittedBranch
						&& b.LocalAheadCount > 0
						&& b.RemoteAheadCount == 0).ToList();

				foreach (Branch branch in pushableBranches)
				{
					progress.SetText($"Push branch {branch.Name} ...");

					await gitService.PushBranchAsync(workingFolder, branch.Name);
				}

				await RefreshAfterCommandAsync(false);
			});
		}


		private bool CanExecuteTryPushAllBranches()
		{
			if (!string.IsNullOrEmpty(ConflictsText))
			{
				return false;
			}

			Branch uncommittedBranch = UnCommited?.Branch;

			return Repository.Branches.Any(
				b => b != uncommittedBranch
				&& b.LocalAheadCount > 0
				&& b.RemoteAheadCount == 0);
		}


		private void PushCurrentBranch()
		{
			isInternalDialog = true;
			Progress.ShowDialog(
				Owner, $"Push current branch {Repository.CurrentBranch.Name} ...", async () =>
			{
				string workingFolder = Repository.MRepository.WorkingFolder;

				CredentialHandler credentialHandler = new CredentialHandler(Owner);

				await gitService.PushNotesAsync(workingFolder, Repository.RootId);

				await gitService.PushCurrentBranchAsync(workingFolder, credentialHandler);

				await RefreshAfterCommandAsync(false);
			});
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


		private Task SetBranchAsync(Commit commit)
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

			isInternalDialog = true;
			if (dialog.ShowDialog() == true)
			{
				Application.Current.MainWindow.Focus();
				string branchName = dialog.IsAutomatically ? null : dialog.PromptText?.Trim();
				string workingFolder = WorkingFolder;

				if (commit.SpecifiedBranchName != branchName)
				{
					Progress.ShowDialog(Owner, $"Set commit branch name {branchName} ...", async () =>
					{
						await repositoryService.SetSpecifiedCommitBranchAsync(workingFolder, commit.Id, branchName);
						if (!string.IsNullOrWhiteSpace(branchName))
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
				await diffService.ShowDiffAsync(commit.Commit.Id, WorkingFolder);
			}
		}


		private async Task UndoCleanWorkingFolderAsync()
		{
			IReadOnlyList<string> failedPaths = new string[0];
			await Task.Yield();

			isInternalDialog = true;
			Progress.ShowDialog(Owner, $"Undo changes and clean working folder {WorkingFolder} ...", async () =>
			{
				failedPaths = await gitService.UndoCleanWorkingFolderAsync(WorkingFolder);

				await RefreshAfterCommandAsync(false);
			});

			if (failedPaths.Any())
			{
				string text = $"Failed to undo and clean working folder.\nSome items where locked:\n";
				foreach (string path in failedPaths.Take(10))
				{
					text += $"\n   {path}";
				}
				if (failedPaths.Count > 10)
				{
					text += "   ...";
				}

				MessageDialog.ShowWarning(Owner, text);
			}

			isInternalDialog = true;
		}


		private async Task UndoUncommittedChangesAsync()
		{
			await Task.Yield();

			isInternalDialog = true;
			Progress.ShowDialog(Owner, $"Undo changes in working folder {WorkingFolder} ...", async () =>
			{
				await gitService.UndoWorkingFolderAsync(WorkingFolder);

				await RefreshAfterCommandAsync(false);
			});

			isInternalDialog = true;
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