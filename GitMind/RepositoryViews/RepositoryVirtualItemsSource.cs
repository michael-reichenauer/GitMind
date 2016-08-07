using System;
using System.Collections.Generic;
using System.Windows;
using GitMind.Utils;
using GitMind.Utils.UI.VirtualCanvas;


namespace GitMind.RepositoryViews
{
	internal class RepositoryVirtualItemsSource : VirtualItemsSource
	{
		private const int minCommitIndex = 0;
		private const int minBranchIndex = 1000000;
		private const int minMergeIndex = 2000000;

		private const int maxCommitIndex = minBranchIndex;
		private const int maxBranchIndex = minMergeIndex;
		private const int maxMergeIndex = 3000000;

		private readonly IReadOnlyList<BranchViewModel> branches;
		private readonly IReadOnlyList<MergeViewModel> merges;
		private readonly IReadOnlyList<CommitViewModel> commits;
		private Rect virtualArea;

		public RepositoryVirtualItemsSource(
			IReadOnlyList<BranchViewModel> branches,
			IReadOnlyList<MergeViewModel> merges,
			IReadOnlyList<CommitViewModel> commits)
		{
			this.branches = branches;
			this.merges = merges;
			this.commits = commits;
		}


		public void DataChanged()
		{
			TriggerInvalidated();
		}

		public void DataChanged(double width)
		{
			virtualArea = new Rect(0, 0, width, Converter.ToRowExtent(commits.Count));
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

			// Get the part of the rectangle that is visible
			viewArea.Intersect(VirtualArea);

			int viewAreaTopIndex = Converter.ToTopRowIndex(viewArea, commits.Count);
			int viewAreaBottomIndex = Converter.ToBottomRowIndex(viewArea, commits.Count);

			if (viewAreaBottomIndex > viewAreaTopIndex)
			{
				// Return visible branches
				for (int i = 0; i < branches.Count; i++)
				{
					BranchViewModel branch = branches[i];

					if (IsVisable(
						viewAreaTopIndex, viewAreaBottomIndex, branch.TipRowIndex, branch.FirstRowIndex))
					{
						yield return i + minBranchIndex;
					}
				}

				// Return visible merges
				for (int i = 0; i < merges.Count; i++)
				{
					MergeViewModel merge = merges[i];
					if (IsVisable(viewAreaTopIndex, viewAreaBottomIndex, merge.ChildRow, merge.ParentRow))
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

			return null;
		}


		private static bool IsVisable(
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
	}
}