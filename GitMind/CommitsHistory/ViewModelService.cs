﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils;


namespace GitMind.CommitsHistory
{
	internal class ViewModelService : IViewModelService
	{
		private readonly IBrushService brushService;


		public ViewModelService()
			: this(new BrushService())
		{
		}

		public ViewModelService(IBrushService brushService)
		{
			this.brushService = brushService;
		}


		public void Update(
			RepositoryViewModel repositoryViewModel,
			IReadOnlyList<string> specifiedBranchNames)
		{
			List<Branch> branches = new List<Branch>();

			foreach (string name in specifiedBranchNames)
			{
				Branch branch = repositoryViewModel.Repository.Branches
					.FirstOrDefault(b => b.Name == name && b.IsActive);
				if (branch != null)
				{
					branches.Add(branch);
				}
			}

			if (!branches.Any())
			{
				Branch currentBranch = repositoryViewModel.Repository.CurrentBranch;

				branches.Add(currentBranch);
			}

			Update(repositoryViewModel, branches);
		}


		public int ToggleMergePoint(RepositoryViewModel repositoryViewModel, Commit commit)
		{
			List<Branch> currentlyShownBranches = GetCurrentlyShownBranches(repositoryViewModel);

			bool isShowing = currentlyShownBranches.Contains(commit.SecondParent.Branch);

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
				BranchViewModel otherBranch = repositoryViewModel.Branches
					.First(b => b.Branch == commit.SecondParent.Branch);

				if (clickedBranch.BranchColumn > otherBranch.BranchColumn)
				{
					// Closing the branch that was clicked on since that is to the right
					otherBranch = clickedBranch;
					stableCommit = commit.SecondParent;
				}

				IEnumerable<Branch> closingBranches = GetBranchAndDescendants(
					currentlyShownBranches, otherBranch.Branch);

				currentlyShownBranches.RemoveAll(b => b.Name != "master" && closingBranches.Contains(b));
			}

			CommitViewModel stableCommitViewModel = repositoryViewModel.CommitsById[stableCommit.Id];

			int currentRow = stableCommitViewModel.RowIndex;
			//	repositoryViewModel.SelectedItem = stableCommitViewModel;
			repositoryViewModel.SelectedIndex = currentRow;
			Update(repositoryViewModel, currentlyShownBranches);

			int newRow = stableCommitViewModel.RowIndex;
			Log.Debug($"Row {currentRow}->{newRow} for {stableCommit}");

			return currentRow - newRow;
		}

		public void ShowBranch(RepositoryViewModel repositoryViewModel, Branch branch)
		{
			List<Branch> currentlyShownBranches = GetCurrentlyShownBranches(repositoryViewModel);

			bool isShowing = currentlyShownBranches.Contains(branch);

			if (!isShowing)
			{
				// Showing the specified branch
				currentlyShownBranches.Add(branch);
				Update(repositoryViewModel, currentlyShownBranches);

				var x = repositoryViewModel.Branches.FirstOrDefault(b => b.Branch == branch);
				if (x != null)
				{
					var y = x.LatestRowIndex;
					repositoryViewModel.ScrollRows(repositoryViewModel.Commits.Count);
					repositoryViewModel.ScrollRows(-(y - 10));
				}

				repositoryViewModel.VirtualItemsSource.DataChanged(repositoryViewModel.Width);

			}


			//int currentRow = repositoryViewModel.CommitsById[stableCommit.Id].RowIndex;
			//Update(repositoryViewModel, currentlyShownBranches);

			//int newRow = repositoryViewModel.CommitsById[stableCommit.Id].RowIndex;
			//Log.Debug($"Row {currentRow}->{newRow} for {stableCommit}");

			//return currentRow - newRow;
		}

		public void HideBranch(RepositoryViewModel repositoryViewModel, Branch branch)
		{
			List<Branch> currentlyShownBranches = GetCurrentlyShownBranches(repositoryViewModel);

			bool isShowing = currentlyShownBranches.Contains(branch);

			if (isShowing)
			{
				IEnumerable<Branch> closingBranches = GetBranchAndDescendants(
					currentlyShownBranches, branch);

				currentlyShownBranches.RemoveAll(b => b.Name != "master" && closingBranches.Contains(b));

				repositoryViewModel.SelectedIndex = 3;
				Update(repositoryViewModel, currentlyShownBranches);


				repositoryViewModel.VirtualItemsSource.DataChanged(repositoryViewModel.Width);
			}

			//int currentRow = repositoryViewModel.CommitsById[stableCommit.Id].RowIndex;
			//Update(repositoryViewModel, currentlyShownBranches);

			//int newRow = repositoryViewModel.CommitsById[stableCommit.Id].RowIndex;
			//Log.Debug($"Row {currentRow}->{newRow} for {stableCommit}");

			//return currentRow - newRow;
		}


		public async Task SetFilterAsync(RepositoryViewModel repositoryViewModel, string filterText)
		{

			if (string.IsNullOrEmpty(filterText))
			{
				List<Branch> branches = repositoryViewModel.SpecifiedBranches.ToList();
				var commit = repositoryViewModel.SelectedItem as CommitViewModel;
				if (commit != null && !branches.Contains(commit.Commit.Branch))
				{
					branches.Add(commit.Commit.Branch);
				}

				Update(repositoryViewModel, branches);
			}
			else
			{
				Timing t = new Timing();
				List<Commit> commits = await GetFilteredCommitsAsync(
					repositoryViewModel, filterText);
				t.Log($"Got filtered {commits.Count} commits");

				if (filterText != repositoryViewModel.FilterText)
				{
					Log.Warn($"Filter has changed {filterText} ->" + $"{repositoryViewModel.FilterText}");
					return;
				}

				//var branches = commits.Select(c => c.Branch).Distinct().ToList();
				Branch[] branches = new Branch[0];
				UpdateBranches(branches, commits, repositoryViewModel);
				UpdateCommits(commits, repositoryViewModel);
				UpdateMerges(branches, repositoryViewModel);
			}
		}

		private static Task<List<Commit>> GetFilteredCommitsAsync(
			RepositoryViewModel repositoryViewModel, string filterText)
		{
			IEnumerable<Commit> commits = null;
			string filteredText = repositoryViewModel.FilteredText;

			if (StartsWith(filterText, filteredText))
			{
				// The previous used filter text is a sub string of the new search, lets just search
				// these commits
				commits = repositoryViewModel.Commits.Select(c => c.Commit).ToList();
			}
			else
			{
				Repository repository = repositoryViewModel.Repository;

				commits = repository.Commits;
			}

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
						|| Contains(c.Branch.Name, filteredText))
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


		public void Update(
			RepositoryViewModel repositoryViewModel, IReadOnlyList<Branch> specifiedBranches)
		{
			specifiedBranches.ForEach(branch => Log.Debug($"Update with {branch}"));

			Timing t = new Timing();
			IReadOnlyList<Branch> branches = GetBranchesIncludingParents(specifiedBranches, repositoryViewModel);
			t.Log("Branches");
			List<Commit> commits = GetCommits(branches);
			t.Log($"Commits count {commits.Count}");

			repositoryViewModel.ActiveBranches.Clear();
			branches
				.Where(b => b.Name != "master")
				.OrderBy(b => b.Name)
				.ForEach(b => repositoryViewModel.ActiveBranches.Add(
					new BranchItem(b, repositoryViewModel.ShowBranchCommand)));

			UpdateBranches(branches, commits, repositoryViewModel);
			t.Log("Updated Branches");

			UpdateCommits(commits, repositoryViewModel);
			t.Log("Updated Commits");

			UpdateMerges(branches, repositoryViewModel);
			t.Log("Updated Merges");

			repositoryViewModel.SpecifiedBranches = specifiedBranches;
		}


		private static List<Branch> GetCurrentlyShownBranches(RepositoryViewModel repositoryViewModel)
		{
			return repositoryViewModel.Branches.Select(b => b.Branch).ToList();
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
			if (y.HasParentBranch && y.ParentBranch == x)
			{
				return -1;
			}
			else if (x.HasParentBranch && x.ParentBranch == y)
			{
				return 1;
			}

			return 0;
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
			IEnumerable<Commit> sourceCommits,
			RepositoryViewModel repositoryViewModel)
		{
			List<CommitViewModel> commits = repositoryViewModel.Commits;
			var commitsById = repositoryViewModel.CommitsById;

			//SetNumberOfItems(commits, sourceCommits.Count(), i => new CommitViewModel(i, null, null));
			commits.Clear();
			commitsById.Clear();
			int graphWidth = repositoryViewModel.GraphWidth;

			foreach (Commit commit in sourceCommits)
			{
				CommitViewModel commitViewModel = repositoryViewModel.VirtualItemsSource.GetOrAdd(
					commit.Id, (id, virtualId) => new CommitViewModel(id, virtualId));

				commitViewModel.HideBranch = () => HideBranch(repositoryViewModel, commit.Branch);
				commitViewModel.RowIndex = commits.Count;

				commits.Add(commitViewModel);
				commitsById[commit.Id] = commitViewModel;

				commitViewModel.Commit = commit;

				commitViewModel.IsCurrent = commit == repositoryViewModel.Repository.CurrentCommit;

				commitViewModel.IsMergePoint =
					commit.IsMergePoint && commit.Branch != commit.SecondParent.Branch;

				commitViewModel.BranchColumn = IndexOf(repositoryViewModel, commit.Branch);

				commitViewModel.Size = commitViewModel.IsMergePoint ? 10 : 6;
				commitViewModel.XPoint = commitViewModel.IsMergePoint
					? 2 + Converter.ToX(commitViewModel.BranchColumn)
					: 4 + Converter.ToX(commitViewModel.BranchColumn);
				commitViewModel.YPoint = commitViewModel.IsMergePoint ? 2 : 4;

				commitViewModel.GraphWidth = graphWidth;
				commitViewModel.Width = repositoryViewModel.Width - 35;
				commitViewModel.Rect = new Rect(
					0, Converter.ToY(commitViewModel.RowIndex), commitViewModel.Width, Converter.ToY(1));

				commitViewModel.Brush = brushService.GetBranchBrush(commit.Branch);
				commitViewModel.BrushInner = commitViewModel.Brush;

				commitViewModel.CommitBranchText = "Hide branch: " + commit.Branch.Name;
				commitViewModel.CommitBranchName = commit.Branch.Name;
				commitViewModel.ToolTip = GetCommitToolTip(commit);
				commitViewModel.SubjectBrush = GetSubjectBrush(commit);
				commitViewModel.Tags = commit.Tags;
				commitViewModel.Tickets = commit.Tickets;
			}
		}


		private void UpdateBranches(
			IEnumerable<Branch> sourceBranches,
			List<Commit> commits,
			RepositoryViewModel repositoryViewModel)
		{
			int maxColumn = 0;
			var branches = repositoryViewModel.Branches;
			//var commits = repositoryViewModel.CommitsById;
			//SetNumberOfItems(branches, sourceBranches.Count(), i => new BranchViewModel(i));

			branches.Clear();
			//int index = 0;
			foreach (Branch sourceBranch in sourceBranches)
			{
				BranchViewModel branch = repositoryViewModel.VirtualItemsSource.GetOrAdd(
					sourceBranch.Id, (id, virtualId) => new BranchViewModel(
						id,
						virtualId,
						repositoryViewModel.ShowBranchCommand,
						repositoryViewModel.HideBranchCommand));

				branch.ActiveBranches = repositoryViewModel.ActiveBranches;
				branch.Branch = sourceBranch;
				branch.Name = sourceBranch.Name;
				branch.LatestRowIndex = commits.FindIndex(c => c == sourceBranch.LatestCommit);
				branch.FirstRowIndex = commits.FindIndex(c => c == sourceBranch.FirstCommit);
				int height = Converter.ToY(branch.FirstRowIndex - branch.LatestRowIndex);

				//branch.BranchColumn = index++;
				branch.BranchColumn = FindBranchColumn(branches, branch);
				maxColumn = Math.Max(branch.BranchColumn, maxColumn);

				branches.Add(branch);

				branch.Rect = new Rect(
					(double)Converter.ToX(branch.BranchColumn) + 6,
					(double)Converter.ToY(branch.LatestRowIndex) + Converter.HalfRow,
					4,
					height);
				branch.Width = branch.Rect.Width;
				branch.Line = $"M 1,0 L 1,{height}";

				branch.Brush = brushService.GetBranchBrush(sourceBranch);

				branch.BranchToolTip = GetBranchToolTip(branch);

				if (sourceBranch.IsMultiBranch)
				{
					branch.MultiBranches = branch.Branch.ChildBranchNames
						.Select(name => new BranchNameItem(
							branch.Branch.LatestCommit.Id, name, repositoryViewModel.SpecifyMultiBranchCommand))
						.ToList();
					branch.IsMultiBranch = sourceBranch.IsMultiBranch;
				}
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
				if (branches.Any(current => column == current.BranchColumn && IsOverlapping(
					current.LatestRowIndex,
					current.FirstRowIndex,
					branch.LatestRowIndex,
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
				.Where(c => c.IsMergePoint && sourceBranches.Contains(c.Commit.SecondParent.Branch))
				.ToList();

			var branchStarts = branches.Where(b =>
				b.Branch.HasParentBranch && sourceBranches.Contains(b.Branch.ParentCommit.Branch))
				.Select(b => b.Branch.FirstCommit)
				.ToList();

			//SetNumberOfItems(merges, mergePoints.Count + branchStarts.Count, _ => new MergeViewModel());
			merges.Clear();
			foreach (CommitViewModel childCommit in mergePoints)
			{
				CommitViewModel parentCommit = commitsById[childCommit.Commit.SecondParent.Id];
				string mergeId = childCommit.ShortId + "-" + parentCommit.ShortId;

				MergeViewModel merge = repositoryViewModel.VirtualItemsSource.GetOrAdd(
					mergeId, (id, virtualId) => new MergeViewModel(id, virtualId));

				AddMerge(merge, merges, branches, childCommit, parentCommit);
			}

			foreach (Commit childCommit in branchStarts)
			{
				CommitViewModel parentCommit = commitsById[childCommit.FirstParent.Id];
				string mergeId = childCommit.ShortId + "-" + parentCommit.ShortId;

				MergeViewModel merge = repositoryViewModel.VirtualItemsSource.GetOrAdd(
					mergeId, (id, virtualId) => new MergeViewModel(id, virtualId));

				AddMerge(merge, merges, branches, commitsById[childCommit.Id], parentCommit);
			}
		}


		private void AddMerge(
			MergeViewModel merge,
			List<MergeViewModel> merges,
			IReadOnlyCollection<BranchViewModel> branches,
			CommitViewModel childCommit,
			CommitViewModel parentCommit)
		{
			merges.Add(merge);

			BranchViewModel childBranch = branches
				.First(b => b.Branch == childCommit.Commit.Branch);
			BranchViewModel parentBranch = branches
				.First(b => b.Branch == parentCommit.Commit.Branch);

			childCommit.BrushInner = brushService.GetDarkerBrush(childCommit.Brush);

			int childRow = childCommit.RowIndex;
			int parentRow = parentCommit.RowIndex;
			int childColumn = childBranch.BranchColumn;
			int parentColumn = parentBranch.BranchColumn;

			if (childColumn == parentColumn)
			{

			}

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
			merge.StrokeDash = "";

		}


		private static string GetCommitToolTip(Commit commit)
		{
			string name = commit.Branch.IsMultiBranch ? "MultiBranch" : commit.Branch.Name;
			string toolTip = $"Commit id: {commit.ShortId}\nBranch: {name}";

			if (commit.Branch.LocalAheadCount > 0)
			{
				toolTip += $"\nAhead: {commit.Branch.LocalAheadCount}";
			}
			if (commit.Branch.RemoteAheadCount > 0)
			{
				toolTip += $"\nBehind: {commit.Branch.RemoteAheadCount}";
			}

			return toolTip;
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


		//if (!string.IsNullOrWhiteSpace(filterText))
		//{
		//	sourceCommits = model.GitRepo.GetAllCommts()
		//		.Where(c => c.Subject.IndexOf(filterText, StringComparison.CurrentCultureIgnoreCase) != -1
		//		|| c.Author.IndexOf(filterText, StringComparison.CurrentCultureIgnoreCase) != -1
		//		|| c.Id.StartsWith(filterText, StringComparison.CurrentCultureIgnoreCase))
		//		.Select(c => model.GetCommit(c.Id))
		//		.ToList();

		//}
		//else
		//{
		//	commitViewModel.SubjectBrush = brushService.SubjectBrush;
		//	commitViewModel.IsMergePoint = false;
		//	commitViewModel.BranchColumn = 0;
		//	commitViewModel.Size = 0;
		//	commitViewModel.XPoint = 0;
		//	commitViewModel.YPoint = 0;
		//	commitViewModel.Brush = Brushes.Black;
		//	commitViewModel.BrushInner = Brushes.Black;
		//	commitViewModel.CommitBranchText = "";
		//	commitViewModel.CommitBranchName = "";
		//	commitViewModel.ToolTip = "";
		//}
		//}

		public Brush GetSubjectBrush(Commit commit)
		{
			Brush subjectBrush;
			if (commit.IsLocalAhead)
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