using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils;


namespace GitMind.RepositoryViews
{
	/// <summary>
	/// ViewModelService
	/// </summary>
	internal class ViewModelService : IViewModelService
	{
		private readonly IBrushService brushService = new BrushService();
		private static readonly int CommitHeight = Converters.ToY(1);


		public void UpdateViewModel(RepositoryViewModel repositoryViewModel)
		{
			Timing t = new Timing();

			List<Branch> specifiedBranches = repositoryViewModel.SpecifiedBranches.ToList();

			foreach (BranchName name in repositoryViewModel.SpecifiedBranchNames)
			{
				Branch branch;

				// First try find active branch with name and then other branch
				if (name != null)
				{
					branch = repositoryViewModel.Repository.Branches
						.FirstOrDefault(b => b.Name == name && b.IsActive)
									 ?? repositoryViewModel.Repository.Branches.FirstOrDefault(b => b.Name == name);
					if (branch != null && !specifiedBranches.Any(b => b.Name == name))
					{
						specifiedBranches.Add(branch);
					}
				}
				else
				{
					branch = repositoryViewModel.Repository.Branches.First(b => b.IsCurrentBranch);
					if (branch != null && !specifiedBranches.Any(b => b.Name == branch.Name))
					{
						specifiedBranches.Add(branch);
					}
				}
			}

			if (!specifiedBranches.Any())
			{
				Branch currentBranch = repositoryViewModel.Repository.CurrentBranch;

				specifiedBranches.Add(currentBranch);
			}

			IReadOnlyList<Branch> branches = GetBranchesIncludingParents(specifiedBranches, repositoryViewModel);

			List<Commit> commits = GetCommits(branches);

			repositoryViewModel.ShownBranches.Clear();
			branches
				.OrderBy(b => b.Name)
				.ForEach(b => repositoryViewModel.ShownBranches.Add(
					new BranchItem(b, repositoryViewModel.ShowBranchCommand, repositoryViewModel.MergeBranchCommand)));

			repositoryViewModel.HidableBranches.Clear();
			branches
				.Where(b => b.Name != BranchName.Master)
				.OrderBy(b => b.Name)
				.ForEach(b => repositoryViewModel.HidableBranches.Add(
					new BranchItem(b, repositoryViewModel.ShowBranchCommand, repositoryViewModel.MergeBranchCommand)));

			repositoryViewModel.ShowableBranches.Clear();
			IEnumerable<Branch> showableBranches = repositoryViewModel.Repository.Branches
				.Where(b => b.IsActive);
			IReadOnlyList<BranchItem> showableBrancheItems = BranchItem.GetBranches(
				showableBranches,
				repositoryViewModel.ShowBranchCommand);
			showableBrancheItems.ForEach(b => repositoryViewModel.ShowableBranches.Add(b));

			repositoryViewModel.DeletableBranches.Clear();
			IEnumerable<Branch> deletableBranches = repositoryViewModel.Repository.Branches
				.Where(b => b.IsActive && b.Name != BranchName.Master);
			IReadOnlyList<BranchItem> deletableBrancheItems = BranchItem.GetBranches(
				deletableBranches,
				repositoryViewModel.DeleteBranchCommand);
			deletableBrancheItems.ForEach(b => repositoryViewModel.DeletableBranches.Add(b));

			UpdateViewModel(repositoryViewModel, branches, commits);

			t.Log("Updated view model");
		}


		private void UpdateViewModel(
			RepositoryViewModel repositoryViewModel,
			IReadOnlyList<Branch> branches,
			List<Commit> commits)
		{
			UpdateBranches(branches, commits, repositoryViewModel);

			UpdateCommits(commits, repositoryViewModel);

			UpdateMerges(branches, repositoryViewModel);

			repositoryViewModel.SpecifiedBranches = branches.ToList();
			repositoryViewModel.SpecifiedBranchNames = new BranchName[0];
		}


		public int ToggleMergePoint(RepositoryViewModel repositoryViewModel, Commit commit)
		{
			List<Branch> currentlyShownBranches = repositoryViewModel.SpecifiedBranches.ToList();

			BranchViewModel clickedBranch = repositoryViewModel
				.Branches.First(b => b.Branch == commit.Branch);

			Commit stableCommit = commit;
			if (commit.HasSecondParent && !currentlyShownBranches.Contains(commit.SecondParent.Branch))
			{
				// Showing the specified branch
				Log.Usage("Open branch");
				Log.Info($"Open branch {commit.SecondParent.Branch}");
				currentlyShownBranches.Add(commit.SecondParent.Branch);
				if (commit.SecondParent.Branch.IsMainPart)
				{
					currentlyShownBranches.Add(commit.SecondParent.Branch.LocalSubBranch);
				}
			}
			else
			{
				// Closing shown branch
				BranchViewModel otherBranch;

				if (commit.HasSecondParent
					&& commit.SecondParent.Branch != commit.Branch
					&& currentlyShownBranches.Contains(commit.SecondParent.Branch))
				{
					otherBranch = repositoryViewModel.Branches
						.First(b => b.Branch == commit.SecondParent.Branch);

					if (clickedBranch.BranchColumn > otherBranch.BranchColumn)
					{
						// Closing the branch that was clicked on since that is to the right
						otherBranch = clickedBranch;
						stableCommit = commit.SecondParent;
					}
				}
				else if (!commit.HasFirstChild || commit.Branch.IsMainPart && commit.Branch.TipCommit == commit)
				{
					// A branch tip, closing the clicked branch
					otherBranch = clickedBranch;
					stableCommit = commit.Branch.ParentCommit;
					if (clickedBranch.Branch.IsLocalPart)
					{
						otherBranch = repositoryViewModel
							.Branches.First(b => b.Branch == clickedBranch.Branch.MainbBranch);
						stableCommit = otherBranch.Branch.ParentCommit;
					}
				}
				else
				{
					otherBranch = repositoryViewModel.Branches
						.First(b => b.Branch == commit.FirstParent.Branch);

					if (clickedBranch.BranchColumn > otherBranch.BranchColumn)
					{
						// Closing the branch that was clicked on since that is to the right
						otherBranch = clickedBranch;
						stableCommit = commit.FirstParent;
					}
				}

				if (otherBranch.Branch.IsLocalPart)
				{
					otherBranch = repositoryViewModel.Branches
						.First(b => b.Branch == otherBranch.Branch.MainbBranch);
					stableCommit = otherBranch.Branch.ParentCommit;
				}

				Log.Usage("Close branch");
				Log.Info($"Close branch {otherBranch.Branch}");
				IEnumerable<Branch> closingBranches = GetBranchAndDescendants(
					currentlyShownBranches, otherBranch.Branch);

				currentlyShownBranches.RemoveAll(b => b.Name != BranchName.Master && closingBranches.Contains(b));
			}

			CommitViewModel stableCommitViewModel = repositoryViewModel.CommitsById[stableCommit.Id];

			int currentRow = stableCommitViewModel.RowIndex;
			repositoryViewModel.SelectedIndex = currentRow;
			repositoryViewModel.SpecifiedBranches = currentlyShownBranches;
			UpdateViewModel(repositoryViewModel);

			CommitViewModel newCommitViewModel = repositoryViewModel.CommitsById[stableCommit.Id];

			int newRow = newCommitViewModel.RowIndex;
			Log.Debug($"Row {currentRow}->{newRow} for {stableCommit}");

			return currentRow - newRow;
		}


		public void ShowBranch(RepositoryViewModel repositoryViewModel, Branch branch)
		{
			List<Branch> currentlyShownBranches = repositoryViewModel.SpecifiedBranches.ToList();

			bool isShowing = currentlyShownBranches.Contains(branch);

			if (!isShowing)
			{
				// Showing the specified branch
				currentlyShownBranches.Add(branch);
				if (branch.IsMainPart)
				{
					currentlyShownBranches.Add(branch.LocalSubBranch);
				}

				repositoryViewModel.SpecifiedBranches = currentlyShownBranches;
				UpdateViewModel(repositoryViewModel);
			}

			var x = repositoryViewModel.Branches.FirstOrDefault(b => b.Branch == branch);
			if (x != null)
			{
				var y = x.TipRowIndex;
				repositoryViewModel.ScrollRows(repositoryViewModel.Commits.Count);
				repositoryViewModel.ScrollRows(-(y - 10));
			}

			repositoryViewModel.VirtualItemsSource.DataChanged(repositoryViewModel.Width);
		}

		public void HideBranch(RepositoryViewModel repositoryViewModel, Branch branch)
		{
			List<Branch> currentlyShownBranches = repositoryViewModel.SpecifiedBranches.ToList();

			bool isShowing = currentlyShownBranches.Contains(branch);

			if (isShowing)
			{
				IEnumerable<Branch> closingBranches = GetBranchAndDescendants(
					currentlyShownBranches, branch);

				if (branch.IsLocalPart)
				{
					closingBranches = closingBranches.Concat(GetBranchAndDescendants(
						currentlyShownBranches,
						currentlyShownBranches.First(b => b.LocalSubBranch == branch)));
					;
				}

				currentlyShownBranches.RemoveAll(b => b.Name != BranchName.Master && closingBranches.Contains(b));

				repositoryViewModel.SpecifiedBranches = currentlyShownBranches;
				UpdateViewModel(repositoryViewModel);

				repositoryViewModel.VirtualItemsSource.DataChanged(repositoryViewModel.Width);
			}
		}


		public async Task SetFilterAsync(RepositoryViewModel repositoryViewModel, string filterText)
		{
			if (string.IsNullOrEmpty(filterText))
			{
				List<Branch> preFilterBranches = repositoryViewModel.PreFilterBranches.ToList();
				CommitViewModel preFilterSelectedItem = repositoryViewModel.PreFilterSelectedItem;
				repositoryViewModel.PreFilterBranches = null;
				repositoryViewModel.PreFilterSelectedItem = null;

				var commit = repositoryViewModel.SelectedItem as CommitViewModel;
				if (commit != null && !preFilterBranches.Contains(commit.Commit.Branch))
				{
					preFilterBranches.Add(commit.Commit.Branch);
				}
				else if (commit == null)
				{
					repositoryViewModel.SelectedItem = preFilterSelectedItem;
				}

				repositoryViewModel.SpecifiedBranches = preFilterBranches;
				UpdateViewModel(repositoryViewModel);
			}
			else
			{
				Timing t = new Timing();
				List<Commit> commits = await GetFilteredCommitsAsync(repositoryViewModel, filterText);
				t.Log($"Got filtered {commits.Count} commits");

				if (repositoryViewModel.PreFilterBranches == null)
				{
					// Storing pre-filter mode state to be used when leaving filter mode
					repositoryViewModel.PreFilterBranches = repositoryViewModel.SpecifiedBranches;
					repositoryViewModel.PreFilterSelectedItem = repositoryViewModel.SelectedItem as CommitViewModel;
					repositoryViewModel.PreFilterSelectedItem = null;
				}

				Branch[] branches = new Branch[0];
				UpdateViewModel(repositoryViewModel, branches, commits);
			}
		}

		private static Task<List<Commit>> GetFilteredCommitsAsync(
			RepositoryViewModel repositoryViewModel, string filterText)
		{
			IEnumerable<Commit> commits = null;

			bool isSearchSpecifiedNames = filterText == "$gm:";
			bool isSearchCommitNames = filterText == "$gm:c";

			Repository repository = repositoryViewModel.Repository;

			commits = repository.Commits;

			Log.Debug($"Searching in {commits.Count()} commits");

			return Task.Run(() =>
			{
				return commits
					.Where(c =>
						StartsWith(c.Id, filterText)
						|| Contains(c.Subject, filterText)
						|| Contains(c.Author, filterText)
						|| Contains(c.AuthorDateText, filterText)
						|| Contains(c.Tickets, filterText)
						|| Contains(c.Tags, filterText)
						|| Contains(c.Branch.Name, filterText)
						|| (isSearchSpecifiedNames && !string.IsNullOrEmpty(c.SpecifiedBranchName))
						|| (isSearchCommitNames && !string.IsNullOrEmpty(c.CommitBranchName)))
					.OrderByDescending(c => c.CommitDate)
					.ToList();
			});
		}


		private static bool Contains(string text, string subText)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(subText))
			{
				return false;
			}

			return text.IndexOf(subText, StringComparison.OrdinalIgnoreCase) != -1;
		}

		private static bool StartsWith(string text, string subText)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(subText))
			{
				return false;
			}

			return text.StartsWith(subText, StringComparison.OrdinalIgnoreCase);
		}


		private static IReadOnlyList<Branch> GetBranchesIncludingParents(
			IEnumerable<Branch> branches, RepositoryViewModel repositoryViewModel)
		{
			Branch masterBranch = GetMasterBranch(repositoryViewModel.Repository);
			Branch[] masterBranches = { masterBranch };

			List<Branch> branchesInRepo = new List<Branch>();
			foreach (Branch branch in branches.Where(b => b != null))
			{
				Branch branchInRepo = repositoryViewModel.Repository.Branches
					.FirstOrDefault(b => b.Id == branch.Id);

				if (branchInRepo != null)
				{
					branchesInRepo.Add(branchInRepo);
					if (branchInRepo.IsMainPart)
					{
						branchesInRepo.Add(branchInRepo.LocalSubBranch);
					}
					else if (branchInRepo.IsLocalPart)
					{
						branchesInRepo.Add(branchInRepo.MainbBranch);
					}
				}
			}

			List<Branch> branchesWithParents = masterBranches.
				Concat(branchesInRepo
					.Concat(branchesInRepo.SelectMany(branch => branch.Parents().Take(10))))
				.Distinct()
				.OrderBy(b => b, Compare.With<Branch>(CompareBranches))
				.ToList();

			return branchesWithParents;
		}

		private static int CompareBranches(Branch x, Branch y)
		{
			if (HasAncestor(y, x))
			{
				return -1;
			}
			else if (HasAncestor(x, y))
			{
				return 1;
			}

			return 0;
		}


		public static bool HasAncestor(Branch branch, Branch ancestor)
		{
			Branch current = branch;

			while (current.HasParentBranch)
			{
				if (current.ParentBranch == ancestor)
				{
					return true;
				}

				current = current.ParentBranch;
			}

			return false;
		}


		private static IEnumerable<Branch> GetBranchAndDescendants(
		IEnumerable<Branch> branches, Branch branch)
		{
			IEnumerable<Branch> children = branches
				.Where(b => b.HasParentBranch && b.ParentBranch == branch);

			return
				new[] { branch }.Concat(children.SelectMany(b => GetBranchAndDescendants(branches, b)));
		}


		private static List<Commit> GetCommits(IEnumerable<Branch> branches)
		{
			return branches
				.SelectMany(branch => branch.Commits)
				.OrderByDescending(commit => commit.CommitDate)
				.ToList();
		}


		private void UpdateCommits(
			IReadOnlyList<Commit> sourceCommits,
			RepositoryViewModel repositoryViewModel)
		{
			List<CommitViewModel> commits = repositoryViewModel.Commits;
			var commitsById = repositoryViewModel.CommitsById;

			SetNumberOfItems(commits, sourceCommits.Count, i => new CommitViewModel(
				repositoryViewModel,
				repositoryViewModel.ToggleDetailsCommand,
				repositoryViewModel.ShowDiffCommand,
				repositoryViewModel.SetBranchCommand,
				repositoryViewModel.UndoCleanWorkingFolderCommand,
				repositoryViewModel.UndoUncommittedChangesCommand));

			commitsById.Clear();
			int graphWidth = repositoryViewModel.GraphWidth;

			int index = 0;
			foreach (Commit commit in sourceCommits)
			{
				CommitViewModel commitViewModel = commits[index];
				commitsById[commit.Id] = commitViewModel;

				commitViewModel.Commit = commit;
				commitViewModel.RowIndex = index++;

				commitViewModel.BranchViewModel = GetBranchViewModel(repositoryViewModel, commit.Branch);

				int x = commitViewModel.BranchViewModel?.X ?? -20;
				int y = Converters.ToY(commitViewModel.RowIndex);

				commitViewModel.XPoint = commitViewModel.IsEndPoint
					? 3 + x
					: commitViewModel.IsMergePoint ? 2 + x : 4 + x;

				commitViewModel.GraphWidth = graphWidth;
				commitViewModel.Width = repositoryViewModel.Width - 35;
				
				commitViewModel.Rect = new Rect(0, y, commitViewModel.Width, CommitHeight);

				commitViewModel.Brush = brushService.GetBranchBrush(commit.Branch);
				commitViewModel.BrushInner = commitViewModel.Brush;
				commitViewModel.SetNormal(GetSubjectBrush(commit));
				commitViewModel.BranchToolTip = GetBranchToolTip(commit.Branch);

				if (commitViewModel.IsMergePoint
					&& !commit.HasSecondParent
					&& (commit == commit.Branch.TipCommit || commit == commit.Branch.FirstCommit))
				{
					commitViewModel.BrushInner = brushService.GetDarkerBrush(commitViewModel.Brush);
				}

				commitViewModel.NotifyAll();
			}
		}


		private void UpdateBranches(
			IReadOnlyList<Branch> sourceBranches,
			List<Commit> commits,
			RepositoryViewModel repositoryViewModel)
		{
			int maxColumn = 0;
			var branches = repositoryViewModel.Branches;

			SetNumberOfItems(branches, sourceBranches.Count, i => new BranchViewModel(
				repositoryViewModel,
				repositoryViewModel.ShowBranchCommand,
				repositoryViewModel.MergeBranchCommand,
				repositoryViewModel.DeleteBranchCommand,
				repositoryViewModel.PublishBranchCommand,
				repositoryViewModel.PushBranchCommand,
				repositoryViewModel.UpdateBranchCommand));

			int index = 0;
			List<BranchViewModel> addedBranchColumns = new List<BranchViewModel>();
			foreach (Branch sourceBranch in sourceBranches)
			{
				BranchViewModel branch = branches[index++];
				branch.Branch = sourceBranch;

				branch.ActiveBranches = repositoryViewModel.HidableBranches;
				branch.ShownBranches = repositoryViewModel.ShownBranches;

				branch.TipRowIndex = commits.FindIndex(c => c == sourceBranch.TipCommit);
				branch.FirstRowIndex = commits.FindIndex(c => c == sourceBranch.FirstCommit);
				int height = Converters.ToY(branch.FirstRowIndex - branch.TipRowIndex) + 8;
			
				branch.BranchColumn = FindFreeBranchColumn(addedBranchColumns, branch);
				maxColumn = Math.Max(branch.BranchColumn, maxColumn);
				addedBranchColumns.Add(branch);

				branch.X = branch.Branch.IsLocalPart && branch.Branch.MainbBranch.Commits.Any()
					? Converters.ToX(branch.BranchColumn) - 10
					: Converters.ToX(branch.BranchColumn);

				branch.Brush = brushService.GetBranchBrush(sourceBranch);
				branch.HoverBrush = Brushes.Transparent;
				branch.Dashes = sourceBranch.IsLocalPart ? "1" : "";

				branch.Rect = new Rect(
					branch.X + 3,
					(double)Converters.ToY(branch.TipRowIndex) + Converters.HalfRow - 6,
					10,
					height + 4);

				int top = sourceBranch == sourceBranch.Repository.CurrentBranch ? -3 : 2;
				branch.Line = $"M 4,{top} L 4,{height}";

				if (branch.FirstRowIndex == branch.TipRowIndex)
				{
					branch.Line = "";
				}

				branch.HoverBrushNormal = branch.Brush;
				branch.HoverBrushHighlight = brushService.GetLighterBrush(branch.Brush);
				branch.DimBrushHighlight = brushService.GetLighterLighterBrush(branch.Brush);
				branch.BranchToolTip = GetBranchToolTip(sourceBranch);
				branch.CurrentBranchName = repositoryViewModel.Repository.CurrentBranch.Name;

				branch.SetNormal();

				branch.NotifyAll();
			}

			repositoryViewModel.GraphWidth = Converters.ToX(maxColumn + 1);
		}


		private string GetBranchToolTip(Branch branch)
		{
			string name = branch.IsMultiBranch ? "MultiBranch" : branch.Name.ToString();
			string toolTip = $"{name}";
			if (branch.IsLocalPart)
			{
				toolTip += " (local part)";
			}

			if (branch.LocalAheadCount > 0)
			{
				toolTip += $"\nLocal branch ahead: {branch.LocalAheadCount}";
			}
			else if (branch.IsLocal || branch.IsMainPart)
			{
				toolTip += "\nLocal branch";
			}

			if (branch.RemoteAheadCount > 0)
			{
				toolTip += $"\nRemote branch ahead: {branch.RemoteAheadCount}";
			}
			else if (branch.IsRemote || branch.IsLocalPart)
			{
				toolTip += "\nRemote branch";
			}

			if (branch.ChildBranchNames.Count > 1)
			{
				toolTip += $"\n\nBranch could be one of:";
				foreach (BranchName branchName in branch.ChildBranchNames)
				{
					toolTip += $"\n   {branchName}";
				}
			}

			return toolTip;
		}


		private int FindFreeBranchColumn(List<BranchViewModel> branches, BranchViewModel branch)
		{
			int column = 0;
			if (branch.Branch.HasParentBranch)
			{
				BranchViewModel parent = branches
					.FirstOrDefault(b => b.Branch == branch.Branch.ParentBranch);
				if (parent != null)
				{
					column = parent.BranchColumn + 1;
				}
			}

			while (true)
			{
				if (branches.Any(current =>
					current.Id != branch.Id
					&& column == current.BranchColumn
					&& IsOverlapping(
						current.TipRowIndex,
						current.FirstRowIndex,
						branch.TipRowIndex - 5,
						branch.FirstRowIndex + 5)))
				{
					column++;
				}
				else
				{
					return column;
				}
			}
		}

		private static bool IsOverlapping(
			int areaTopIndex,
			int areaBottomIndex,
			int itemTopIndex,
			int ItemBottomIndex)
		{
			return
				(itemTopIndex >= areaTopIndex && itemTopIndex <= areaBottomIndex)
					|| (ItemBottomIndex >= areaTopIndex && ItemBottomIndex <= areaBottomIndex)
					|| (itemTopIndex <= areaTopIndex && ItemBottomIndex >= areaBottomIndex);
		}


		private void UpdateMerges(
			IEnumerable<Branch> sourceBranches,
			RepositoryViewModel repositoryViewModel)
		{
			var branches = repositoryViewModel.Branches;
			var commits = repositoryViewModel.Commits;
			var commitsById = repositoryViewModel.CommitsById;
			var merges = repositoryViewModel.Merges;

			var mergePoints = commits
				.Where(c => c.IsMergePoint && c.Commit.HasSecondParent && sourceBranches.Contains(c.Commit.SecondParent.Branch))
				.ToList();

			var branchStarts = branches.Where(b =>
				b.Branch.HasParentBranch && sourceBranches.Contains(b.Branch.ParentCommit.Branch))
				.Select(b => b.Branch.FirstCommit)
				.ToList();

			bool isMergeInProgress =
				repositoryViewModel.Repository.Status.IsMerging
				&& branches.Any(b => b.Branch == repositoryViewModel.Repository.CurrentBranch)
				&& repositoryViewModel.MergingBranch != null
				&& branches.Any(b => b.Branch.Id == repositoryViewModel.MergingBranch.Id)
				&& repositoryViewModel.Repository.Commits.Contains(Commit.UncommittedId);

			int mergeCount = mergePoints.Count + branchStarts.Count + (isMergeInProgress ? 1 : 0);

			SetNumberOfItems(merges, mergeCount, _ => new MergeViewModel());

			int index = 0;
			foreach (CommitViewModel childCommit in mergePoints)
			{
				CommitViewModel parentCommit = commitsById[childCommit.Commit.SecondParent.Id];

				MergeViewModel merge = merges[index++];

				SetMerge(merge, branches, childCommit, parentCommit);
			}

			foreach (Commit childCommit in branchStarts)
			{
				CommitViewModel parentCommit = commitsById[childCommit.FirstParent.Id];

				MergeViewModel merge = merges[index++];

				SetMerge(merge, branches, commitsById[childCommit.Id], parentCommit, false);
			}

			if (isMergeInProgress)
			{
				string mergeSourceId = repositoryViewModel.MergingBranch.TipCommit.Id;
				CommitViewModel parentCommit = commitsById[mergeSourceId];
				MergeViewModel merge = merges[index++];
				SetMerge(merge, branches, commitsById[Commit.UncommittedId], parentCommit);
			}
		}


		private void SetMerge(
			MergeViewModel merge,
			IReadOnlyCollection<BranchViewModel> branches,
			CommitViewModel childCommit,
			CommitViewModel parentCommit,
			bool isMerge = true)
		{
			BranchViewModel childBranch = branches
				.First(b => b.Branch == childCommit.Commit.Branch);
			BranchViewModel parentBranch = branches
				.First(b => b.Branch == parentCommit.Commit.Branch);

			if (isMerge)
			{
				childCommit.BrushInner = brushService.GetDarkerBrush(childCommit.Brush);
			}

			int childRow = childCommit.RowIndex;
			int parentRow = parentCommit.RowIndex;
			int childColumn = childBranch.BranchColumn;
			int parentColumn = parentBranch.BranchColumn;

			bool isBranchStart = (childCommit.Commit.HasFirstParent
				&& childCommit.Commit.FirstParent.Branch != childCommit.Commit.Branch)
				|| (childCommit.Commit.Branch.IsLocalPart && parentCommit.Commit.Branch.IsMainPart);

			BranchViewModel mainBranch = childColumn >= parentColumn ? childBranch : parentBranch;

			int childX = childCommit.X;
			int parentX = parentCommit.X;
		
			int x1 = childX <= parentX ? 0 : childX - parentX - 6;
			int y1 = 0;
			int x2 = parentX <= childX ? 0 : parentX - childX - 6;
			int y2 = Converters.ToY(parentRow - childRow) + Converters.HalfRow - 8;

			if (isBranchStart && x1 != x2)
			{
				y1 = y1 + 2;
				x1 = x1 + 2;
			}

			merge.ChildRow = childRow;
			merge.ParentRow = parentRow;

			double y = (double)Converters.ToY(childRow);

			merge.Rect = new Rect(
				(double)Math.Min(childX, parentX) + 10,
				y + Converters.HalfRow,
				Math.Abs(childX - parentX) + 2 + (x1 == x2 ? 2 : 0),
				y2 + 2);
			merge.Width = merge.Rect.Width;

			merge.Line = $"M {x1},{y1} L {x2},{y2}";
			merge.Brush = mainBranch.Brush;
			merge.Stroke = isBranchStart ? 2 : 1;
			merge.StrokeDash =
				(childCommit.Commit.Branch.IsLocalPart && parentCommit.Commit.Branch.IsMainPart)
				|| (parentCommit.Commit.Branch.IsLocalPart && childCommit.Commit.Branch.IsMainPart)
				? "1" : "";

			merge.NotifyAll();
		}


		private static Branch GetMasterBranch(Repository repository)
		{
			return repository.Branches.First(b => b.Name == BranchName.Master && b.IsActive);
		}


		private BranchViewModel GetBranchViewModel(
			RepositoryViewModel repositoryViewModel, Branch branch)
		{
			foreach (BranchViewModel current in repositoryViewModel.Branches)
			{
				if (current.Branch == branch)
				{
					return current;
				}
			}

			return null;
		}


		public Brush GetSubjectBrush(Commit commit)
		{
			Brush subjectBrush;
			if (commit.HasConflicts)
			{
				subjectBrush = BrushService.ConflictBrush;
			}
			else if (commit.IsMerging)
			{
				subjectBrush = BrushService.MergeBrush;
			}
			else if (commit.IsUncommitted)
			{
				subjectBrush = brushService.UnCommittedBrush;
			}
			else if (commit.IsLocalAhead && commit.Branch.LocalAheadCount > 0)
			{
				subjectBrush = brushService.LocalAheadBrush;
			}
			else if (commit.IsRemoteAhead && commit.Branch.RemoteAheadCount > 0)
			{
				subjectBrush = brushService.RemoteAheadBrush;
			}
			//else if (commit.CommitBranchName != null)
			//{
			//	subjectBrush = Brushes.Fuchsia;
			//}
			//else if (commit.SpecifiedBranchName != null)
			//{
			//	subjectBrush = Brushes.Chocolate;
			//}
			else
			{
				subjectBrush = brushService.SubjectBrush;
			}

			return subjectBrush;
		}


		private void SetNumberOfItems<T>(
			List<T> items, int count, Func<int, T> factory)
		{
			if (items.Count > count)
			{
				// To many items, lets remove the items no longer used
				items.RemoveRange(count, items.Count - count);
			}

			if (items.Count < count)
			{
				// To few items, lets create the rows needed
				for (int i = items.Count; i < count; i++)
				{
					items.Add(factory(i));
				}
			}
		}
	}
}