using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GitMind.RepositoryViews.Open;
using GitMind.Utils.UI.VirtualCanvas;


namespace GitMind.RepositoryViews.Private
{
	internal class RepositoryVirtualItemsSource : VirtualItemsSource
	{
		private const int minCommitIndex = 0;
		private const int minBranchIndex = 1000000;
		private const int minMergeIndex = 2000000;
		private const int minOpenRepoIndex = 3000000;


		private const int maxCommitIndex = minBranchIndex;
		private const int maxBranchIndex = minMergeIndex;
		private const int maxMergeIndex = minOpenRepoIndex;
		private const int maxOpenRepoIndex = 4000000;

		private readonly IReadOnlyList<BranchViewModel> branches;
		private readonly IReadOnlyList<MergeViewModel> merges;
		private readonly IReadOnlyList<CommitViewModel> commits;
		private readonly IReadOnlyList<OpenRepoViewModel> openRepos;
		private Rect virtualArea;

		public RepositoryVirtualItemsSource(
			IReadOnlyList<BranchViewModel> branches,
			IReadOnlyList<MergeViewModel> merges,
			IReadOnlyList<CommitViewModel> commits,
			IReadOnlyList<OpenRepoViewModel> openRepos)
		{
			this.branches = branches;
			this.merges = merges;
			this.commits = commits;
			this.openRepos = openRepos;
		}


		public void DataChanged()
		{
			TriggerInvalidated();
		}

		public void DataChanged(double width)
		{
			virtualArea = new Rect(0, 0, width, Converters.ToRowExtent(commits.Count));
			TriggerInvalidated();
		}


		protected override Rect VirtualArea => virtualArea;


		/// <summary>
		/// Returns range of item ids, which are visible in the area currently shown
		/// </summary>
		protected override IEnumerable<int> GetItemIds(Rect viewArea)
		{
			if (VirtualArea == Rect.Empty || viewArea == Rect.Empty)
			{
				yield break;
			}

			if (openRepos.Any())
			{
				yield return 0 + minOpenRepoIndex;
				yield break;
			}

			// Get the part of the rectangle that is visible
			viewArea.Intersect(VirtualArea);

			int viewAreaTopIndex = Converters.ToTopRowIndex(viewArea, commits.Count);
			int viewAreaBottomIndex = Converters.ToBottomRowIndex(viewArea, commits.Count);

			if (viewAreaBottomIndex > viewAreaTopIndex)
			{
				// Return visible branches
				for (int i = 0; i < branches.Count; i++)
				{
					BranchViewModel branch = branches[i];

					if (IsVisible(
						viewAreaTopIndex, viewAreaBottomIndex, branch.TipRowIndex, branch.FirstRowIndex))
					{
						yield return i + minBranchIndex;
					}
				}

				// Return visible merges
				for (int i = 0; i < merges.Count; i++)
				{
					MergeViewModel merge = merges[i];
					if (IsVisible(viewAreaTopIndex, viewAreaBottomIndex, merge.ChildRow, merge.ParentRow))
					{
						yield return i + minMergeIndex;
					}
				}

				// Return visible commits
				for (int i = viewAreaTopIndex; i <= viewAreaBottomIndex; i++)
				{
					if (i >= 0 && i < commits.Count)
					{
						yield return i + minCommitIndex;
					}
				}
			}
		}


		/// <summary>
		/// Returns the item (commit, branch, merge) corresponding to the specified id.
		/// Commits are in the 0->branchBaseIndex-1 range
		/// Branches are in the branchBaseIndex->mergeBaseIndex-1 range
		/// Merges are mergeBaseIndex-> ... range
		/// </summary>
		protected override object GetItem(int virtualId)
		{
			if (virtualId >= minCommitIndex && virtualId < maxCommitIndex)
			{
				int commitIndex = virtualId - minCommitIndex;
				if (commitIndex < commits.Count)
				{
					return commits[commitIndex];
				}
			}
			else if (virtualId >= minBranchIndex && virtualId < maxBranchIndex)
			{
				int branchIndex = virtualId - minBranchIndex;
				if (branchIndex < branches.Count)
				{
					return branches[branchIndex];
				}
			}
			else if (virtualId >= minMergeIndex && virtualId < maxMergeIndex)
			{
				int mergeIndex = virtualId - minMergeIndex;
				if (mergeIndex < merges.Count)
				{
					return merges[mergeIndex];
				}
			}
			else if (virtualId >= minOpenRepoIndex && virtualId < maxOpenRepoIndex)
			{
				int openIndex = virtualId - minOpenRepoIndex;
				if (openIndex < openRepos.Count)
				{
					return openRepos[openIndex];
				}
			}

			return null;
		}


		private static bool IsVisible(
			int areaTopIndex,
			int areaBottomIndex,
			int itemTopIndex,
			int itemBottomIndex)
		{
			return
				(itemTopIndex >= areaTopIndex && itemTopIndex <= areaBottomIndex)
				|| (itemBottomIndex >= areaTopIndex && itemBottomIndex <= areaBottomIndex)
				|| (itemTopIndex <= areaTopIndex && itemBottomIndex >= areaBottomIndex);
		}
	}
}