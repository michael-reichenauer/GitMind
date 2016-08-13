﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils;


namespace GitMind.RepositoryViews
{
	internal class ViewModelService : IViewModelService
	{
		private readonly IBrushService brushService = new BrushService();


		public void UpdateViewModel(RepositoryViewModel repositoryViewModel)
		{
			Timing t = new Timing();

			List<Branch> specifiedBranches = repositoryViewModel.SpecifiedBranches.ToList();

			foreach (string name in repositoryViewModel.SpecifiedBranchNames)
			{
				// First try find active branch with name and then other branch
				Branch branch = repositoryViewModel.Repository.Branches
					.FirstOrDefault(b => b.Name == name && b.IsActive)
					?? repositoryViewModel.Repository.Branches.FirstOrDefault(b => b.Name == name);

				if (branch != null && !specifiedBranches.Any(b => b.Name == name))
				{
					specifiedBranches.Add(branch);
				}
			}

			if (!specifiedBranches.Any())
			{
				Branch currentBranch = repositoryViewModel.Repository.CurrentBranch;

				specifiedBranches.Add(currentBranch);
			}


			specifiedBranches.ForEach(branch => Log.Debug($"Update with {branch}"));

			IReadOnlyList<Branch> branches = GetBranchesIncludingParents(specifiedBranches, repositoryViewModel);

			List<Commit> commits = GetCommits(branches);

			repositoryViewModel.ShownBranches.Clear();
			branches
				.OrderBy(b => b.Name)
				.ForEach(b => repositoryViewModel.ShownBranches.Add(
					new BranchItem(b, repositoryViewModel.ShowBranchCommand, repositoryViewModel.MergeBranchCommand)));

			repositoryViewModel.HidableBranches.Clear();
			branches
				.Where(b => b.Name != "master")
				.OrderBy(b => b.Name)
				.ForEach(b => repositoryViewModel.HidableBranches.Add(
					new BranchItem(b, repositoryViewModel.ShowBranchCommand, repositoryViewModel.MergeBranchCommand)));

			repositoryViewModel.ShowableBranches.Clear();

			IReadOnlyList < BranchItem > showableBranches = BranchItem.GetBranches(
			repositoryViewModel.Repository.Branches
				.Where(b => b.IsActive && b.Name != "master")
				.Where(b => !repositoryViewModel.HidableBranches.Any(ab => ab.Branch.Id == b.Id)),
				repositoryViewModel.ShowBranchCommand,
				repositoryViewModel.MergeBranchCommand);

			showableBranches.ForEach(b => repositoryViewModel.ShowableBranches.Add(b));

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
			repositoryViewModel.SpecifiedBranchNames = new string[0];
		}


		public int ToggleMergePoint(RepositoryViewModel repositoryViewModel, Commit commit)
		{
			List<Branch> currentlyShownBranches = repositoryViewModel.SpecifiedBranches.ToList();

			bool isShowing =
				(commit.HasSecondParent && currentlyShownBranches.Contains(commit.SecondParent.Branch))
				|| (commit.HasFirstParent && commit.Branch != commit.FirstParent.Branch && currentlyShownBranches.Contains(commit.FirstParent.Branch));

			BranchViewModel clickedBranch = repositoryViewModel
				.Branches.First(b => b.Branch == commit.Branch);

			Commit stableCommit = commit;
			if (!isShowing)
			{
				// Showing the specified branch
				currentlyShownBranches.Add(commit.SecondParent.Branch);
			}
			else
			{
				// Closing shown branch
				BranchViewModel otherBranch;

				if (commit.HasSecondParent)
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

				IEnumerable<Branch> closingBranches = GetBranchAndDescendants(
					currentlyShownBranches, otherBranch.Branch);

				currentlyShownBranches.RemoveAll(b => b.Name != "master" && closingBranches.Contains(b));
			}

			CommitViewModel stableCommitViewModel = repositoryViewModel.CommitsById[stableCommit.Id];

			int currentRow = stableCommitViewModel.RowIndex;
			//	repositoryViewModel.SelectedItem = stableCommitViewModel;
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

				currentlyShownBranches.RemoveAll(b => b.Name != "master" && closingBranches.Contains(b));

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
						|| (isSearchSpecifiedNames && !string.IsNullOrEmpty(c.SpecifiedBranchName)))
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
			foreach (Branch branch in branches)
			{
				Branch branchInRepo = repositoryViewModel.Repository.Branches
					.FirstOrDefault(b => b.Id == branch.Id);

				if (branchInRepo != null)
				{
					branchesInRepo.Add(branchInRepo);
				}
			}

			List<Branch> branchesWithParents = masterBranches.
				Concat(branchesInRepo
					.Concat(branchesInRepo.SelectMany(branch => branch.Parents().Take(10))))
				.Distinct()
				.OrderBy(b => b, Compare.With<Branch>(CompareBranches))
				.ToList();

			branchesWithParents.ForEach(branch => Log.Debug($"Branches with parent with {branch}"));

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
				repositoryViewModel.ToggleDetailsCommand,
				repositoryViewModel.ShowDiffCommand,
				repositoryViewModel.SetBranchCommand,
				repositoryViewModel.SwitchToCommitCommand,
				repositoryViewModel.SwitchBranchCommand,
				repositoryViewModel.CreateBranchFromCommitCommand));

			commitsById.Clear();
			int graphWidth = repositoryViewModel.GraphWidth;

			int index = 0;
			foreach (Commit commit in sourceCommits)
			{
				CommitViewModel commitViewModel = commits[index];
				commitsById[commit.Id] = commitViewModel;

				commitViewModel.Commit = commit;
				commitViewModel.RowIndex = index++;

				commitViewModel.BranchColumn = IndexOf(repositoryViewModel, commit.Branch);

				commitViewModel.XPoint = commitViewModel.IsMergePoint
					? 2 + Converter.ToX(commitViewModel.BranchColumn)
					: 4 + Converter.ToX(commitViewModel.BranchColumn);

				commitViewModel.GraphWidth = graphWidth;
				commitViewModel.Width = repositoryViewModel.Width - 35;
				commitViewModel.Rect = new Rect(
					0, Converter.ToY(commitViewModel.RowIndex), commitViewModel.Width, Converter.ToY(1));

				commitViewModel.Brush = brushService.GetBranchBrush(commit.Branch);
				commitViewModel.BrushInner = commitViewModel.Brush;
				commitViewModel.SetNormal(GetSubjectBrush(commit));

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
				repositoryViewModel.ShowBranchCommand,
				repositoryViewModel.SwitchBranchCommand,
				repositoryViewModel.MergeBranchCommand,
				repositoryViewModel.CreateBranchCommand));

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
				int height = Converter.ToY(branch.FirstRowIndex - branch.TipRowIndex) + 8;

				branch.BranchColumn = FindBranchColumn(addedBranchColumns, branch);
				addedBranchColumns.Add(branch);
				maxColumn = Math.Max(branch.BranchColumn, maxColumn);

				branch.SetNormal();

				branch.Brush = brushService.GetBranchBrush(sourceBranch);
				branch.HoverBrush = Brushes.Transparent;
				branch.Rect = new Rect(
					(double)Converter.ToX(branch.BranchColumn) + 3,
					(double)Converter.ToY(branch.TipRowIndex) + Converter.HalfRow - 6,
					10,
					height + 4);

				branch.Line = $"M 4,2 L 4,{height}";

				branch.HoverBrushNormal = branch.Brush;
				branch.HoverBrushHighlight = brushService.GetLighterBrush(branch.Brush);
				branch.BranchToolTip = GetBranchToolTip(branch);

				branch.NotifyAll();
			}

			repositoryViewModel.GraphWidth = Converter.ToX(maxColumn + 1);
		}


		private string GetBranchToolTip(BranchViewModel branch)
		{
			string name = branch.Branch.IsMultiBranch ? "MultiBranch" : branch.Branch.Name;
			string toolTip = $"Branch: {name}";

			if (branch.Branch.LocalAheadCount > 0)
			{
				toolTip += $"\nAhead: {branch.Branch.LocalAheadCount}";
			}
			if (branch.Branch.RemoteAheadCount > 0)
			{
				toolTip += $"\nBehind: {branch.Branch.RemoteAheadCount}";
			}

			if (branch.Branch.ChildBranchNames.Count > 1)
			{
				toolTip += $"\n\nBranch could be one of:";
				foreach (string branchName in branch.Branch.ChildBranchNames)
				{
					toolTip += $"\n   {branchName}";
				}
			}

			return toolTip;
		}


		private int FindBranchColumn(List<BranchViewModel> branches, BranchViewModel branch)
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
						branch.TipRowIndex,
						branch.FirstRowIndex)))
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

			SetNumberOfItems(merges, mergePoints.Count + branchStarts.Count, _ => new MergeViewModel());

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

				SetMerge(merge, branches, commitsById[childCommit.Id], parentCommit);
			}
		}


		private void SetMerge(
			MergeViewModel merge,
			IReadOnlyCollection<BranchViewModel> branches,
			CommitViewModel childCommit,
			CommitViewModel parentCommit)
		{
			BranchViewModel childBranch = branches
				.First(b => b.Branch == childCommit.Commit.Branch);
			BranchViewModel parentBranch = branches
				.First(b => b.Branch == parentCommit.Commit.Branch);

			childCommit.BrushInner = brushService.GetDarkerBrush(childCommit.Brush);

			int childRow = childCommit.RowIndex;
			int parentRow = parentCommit.RowIndex;
			int childColumn = childBranch.BranchColumn;
			int parentColumn = parentBranch.BranchColumn;

			bool isBranchStart = childCommit.Commit.HasFirstParent
				&& childCommit.Commit.FirstParent.Branch != childCommit.Commit.Branch;

			BranchViewModel mainBranch = childColumn >= parentColumn ? childBranch : parentBranch;

			int childX = Converter.ToX(childColumn);
			int parentX = Converter.ToX(parentColumn);

			int x1 = childX <= parentX ? 0 : childX - parentX - 6;
			int y1 = 0;
			int x2 = parentX <= childX ? 0 : parentX - childX - 6;
			int y2 = Converter.ToY(parentRow - childRow) + Converter.HalfRow - 8;

			if (isBranchStart && x1 != x2)
			{
				y1 = y1 + 2;
				x1 = x1 + 2;
			}

			merge.ChildRow = childRow;
			merge.ParentRow = parentRow;

			double y = (double)Converter.ToY(childRow);

			merge.Rect = new Rect(
				(double)Math.Min(childX, parentX) + 10,
				y + Converter.HalfRow,
				Math.Abs(childX - parentX) + 2 + (x1 == x2 ? 2 : 0),
				y2 + 2);
			merge.Width = merge.Rect.Width;

			merge.Line = $"M {x1},{y1} L {x2},{y2}";
			merge.Brush = mainBranch.Brush;
			merge.Stroke = isBranchStart ? 2 : 1;

			merge.NotifyAll();
		}


		private static Branch GetMasterBranch(Repository repository)
		{
			return repository.Branches.First(b => b.Name == "master" && b.IsActive);
		}


		private int IndexOf(RepositoryViewModel repositoryViewModel, Branch branch)
		{
			foreach (BranchViewModel current in repositoryViewModel.Branches)
			{
				if (current.Branch == branch)
				{
					return current.BranchColumn;
				}
			}

			return -1;
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
			else if (commit.IsLocalAhead)
			{
				subjectBrush = brushService.LocalAheadBrush;
			}
			else if (commit.IsRemoteAhead)
			{
				subjectBrush = brushService.RemoteAheadBrush;
			}
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