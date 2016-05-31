using System;
using System.Collections.Generic;
using System.Linq;
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


		public void Update(RepositoryViewModel repositoryViewModel, Repository repository)
		{
			Timing t = new Timing();
			Branch branch = GetMasterBranch(repository);

			Branch branch50 = repository.Branches.First(b => b.Name == "releases/ACS/5_00" && b.IsActive);
			Update(repositoryViewModel, new[] { branch, branch50 });
			t.Log("Updated repository");
		}


		private void Update(
			RepositoryViewModel repositoryViewModel, IReadOnlyList<Branch> specifiedBranches)
		{
			Timing t = new Timing();
			IEnumerable<Branch> branches = GetBranchesIncludingParents(specifiedBranches);
			t.Log("Branches");
			IEnumerable<Commit> commits = GetCommits(branches);
			t.Log("Commits");
			List<Commit> list = commits.ToList();
			t.Log("Commits2");

			UpdateCommits(list, branches, repositoryViewModel);
			t.Log("Updated Commits");
			UpdateBranches(branches, repositoryViewModel);
			t.Log("Updated Branches");
			UpdateMerges(branches, repositoryViewModel);
			t.Log("Updated Merges");
		}


		private static IEnumerable<Branch> GetBranchesIncludingParents(IEnumerable<Branch> branches)
		{
			return branches
				.Concat(branches.SelectMany(branch => branch.Parents()))
				.Distinct();
		}


		private static IEnumerable<Commit> GetCommits(IEnumerable<Branch> branches)
		{
			return branches
				.SelectMany(branch => branch.Commits)
				.OrderByDescending(commit => commit.CommitDate);
		}


		private void UpdateCommits(
			IEnumerable<Commit> sourceCommits,
			IEnumerable<Branch> branches,
			RepositoryViewModel repositoryViewModel)
		{
			int rowIndex = 0;
			foreach (Commit commit in sourceCommits)
			{
				CommitViewModel commitViewModel = GetCommitViewMode(repositoryViewModel, rowIndex++, commit);

				// commitViewModel.IsCurrent = commit == model.CurrentCommit;

				commitViewModel.IsMergePoint = 
					commit.IsMergePoint && commit.Branch != commit.SecondParent.Branch;

				commitViewModel.BranchColumn = IndexOf(branches, commit.Branch);
				
				commitViewModel.Size = commitViewModel.IsMergePoint ? 10 : 6;
				commitViewModel.XPoint = commitViewModel.IsMergePoint
					? 2 + Converter.ToX(commitViewModel.BranchColumn)
					: 4 + Converter.ToX(commitViewModel.BranchColumn);
				commitViewModel.YPoint = commitViewModel.IsMergePoint ? 2 : 4;

				commitViewModel.Brush = brushService.GetBranchBrush(commit.Branch);
				commitViewModel.BrushInner = commitViewModel.Brush;

				//commitViewModel.BrushInner = commit.IsExpanded
				//	? brushService.GetDarkerBrush(commitViewModel.Brush)
				//	: commitViewModel.Brush;

				commitViewModel.CommitBranchText = "Hide branch: " + commit.Branch.Name;
				commitViewModel.CommitBranchName = commit.Branch.Name;
				//commitViewModel.ToolTip = GetCommitToolTip(commit);
				commitViewModel.SubjectBrush = GetSubjectBrush(commit);
			
				//commitViewModel.Subject = GetSubjectWithoutTickets(commit);
				//commitViewModel.Tags = GetTags(commit);
				//commitViewModel.Tickets = GetTickets(commit);
				//commitIdToRowIndex[commit.Id] = rowIndex;
			}

			RemoveUnusedCommits(repositoryViewModel, rowIndex);
		}


		private void UpdateBranches(
			IEnumerable<Branch> sourceBranches,
			RepositoryViewModel repositoryViewModel)
		{
			var branches = repositoryViewModel.Branches;
			var commits = repositoryViewModel.CommitsById;
			SetNumberOfItems(branches, sourceBranches.Count(), i => new BranchViewModel(i));

			int index = 0;
			foreach (Branch sourceBranch in sourceBranches)
			{
				BranchViewModel branch = branches[index++];
				branch.Branch = sourceBranch;
				branch.Name = sourceBranch.Name;
				branch.LatestRowIndex = commits[sourceBranch.LatestCommit.Id].RowIndex;
				branch.FirstRowIndex = commits[sourceBranch.FirstCommit.Id].RowIndex;
				int height = Converter.ToY(branch.FirstRowIndex - branch.LatestRowIndex);

				branch.Rect = new Rect(
					(double)Converter.ToX(branch.BranchColumn) + 5,
					(double)Converter.ToY(branch.LatestRowIndex) + Converter.HalfRow,
					6,
					height);
				branch.Width = branch.Rect.Width;
				branch.Line = $"M 2,0 L 2,{height}";

				branch.Brush = brushService.GetBranchBrush(sourceBranch);

				//	branchToolTip: GetBranchToolTip(branch));
			}

			repositoryViewModel.GraphWidth = Converter.ToX(branches.Count());
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
				.Where(c => c.IsMergePoint && sourceBranches.Contains(c.Commit.SecondParent.Branch)
				|| c.Commit.HasFirstParent && c.Commit.Branch != c.Commit.FirstParent.Branch).ToList();
			SetNumberOfItems(merges, mergePoints.Count(), _ => new MergeViewModel());

			int index = 0;
			foreach (CommitViewModel commit in mergePoints)
			{
				MergeViewModel merge = merges[index++];

				CommitViewModel parentCommit = commit.Commit.HasSecondParent 
					? commitsById[commit.Commit.SecondParent.Id] : commitsById[commit.Commit.FirstParent.Id];
				BranchViewModel childBranch = branches
					.First(b => b.Branch == commit.Commit.Branch);
				BranchViewModel parentBranch = branches
					.First(b => b.Branch == parentCommit.Commit.Branch);

				int childRow = commit.RowIndex;
				int parentRow = parentCommit.RowIndex;
				int childColumn = childBranch.BranchColumn;
				int parentColumn = parentBranch.BranchColumn;
				bool isBranchStart = !commit.Commit.HasSecondParent;

				BranchViewModel mainBranch = childColumn > parentColumn ? childBranch : parentBranch;

				int childX = Converter.ToX(childColumn);
				int parentX = Converter.ToX(parentColumn);

				int x1 = childX < parentX ? 0 : childX - parentX - 6;
				int y1 = 0;
				int x2 = parentX < childX ? 0 : parentX - childX - 6;
				int y2 = Converter.ToY(parentRow - childRow) + Converter.HalfRow - 8;

				if (isBranchStart)
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
					Math.Abs(childX - parentX) + 2,
					y2 + 2);
				merge.Width = merge.Rect.Width;

				merge.Line = $"M {x1},{y1} L {x2},{y2}";
				merge.Brush = mainBranch.Brush;
				merge.Stroke = isBranchStart ? 2 : 1;
				merge.StrokeDash = "";
			}

		}


		private static Branch GetMasterBranch(Repository repository)
		{
			return repository.Branches.First(b => b.Name == "master" && b.IsActive);
		}


		private int IndexOf(IEnumerable<Branch> branches, Branch branch)
		{
			int index = 0;
			foreach (Branch current in branches)
			{
				if (current == branch)
				{
					return index;
				}

				index++;
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
			Brush subjectBrush = brushService.SubjectBrush;
			//if (commit.IsLocalAhead)
			//{
			//	subjectBrush = brushService.LocalAheadBrush;
			//}
			//else if (commit.IsRemoteAhead)
			//{
			//	subjectBrush = brushService.RemoteAheadBrush;
			//}

			return subjectBrush;
		}

	
		private static CommitViewModel GetCommitViewMode(
			RepositoryViewModel repositoryViewModel, int index, Commit commit)
		{
			CommitViewModel commitViewModel;
			if (repositoryViewModel.Commits.Count < index)
			{
				commitViewModel = repositoryViewModel.Commits[index];
			}
			else
			{
				commitViewModel = new CommitViewModel(index, null, null);
				repositoryViewModel.Commits.Add(commitViewModel);
			}

			repositoryViewModel.CommitsById[commit.Id] = commitViewModel;
			commitViewModel.Commit = commit;

			return commitViewModel;
		}


		private static void RemoveUnusedCommits(RepositoryViewModel repositoryViewModel, int newCount)
		{
			int count = repositoryViewModel.Commits.Count;
			for (int i = count - 1; i >= newCount; i--)
			{
				CommitViewModel commit = repositoryViewModel.Commits[i];
				repositoryViewModel.CommitsById.Remove(commit.Id);
				repositoryViewModel.Commits.RemoveAt(i);
			}
		}


		private void SetNumberOfItems<T>(List<T> items, int count, Func<int, T> factory)
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