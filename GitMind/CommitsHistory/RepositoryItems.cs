using System;
using System.Collections.Generic;
using System.Windows;
using GitMind.VirtualCanvas;


namespace GitMind.CommitsHistory
{
	internal class RepositoryItems : IVirtualItems
	{
		private static readonly int branchBaseIndex = 1000000;
		private static readonly int mergeBaseIndex = 2000000;

		private readonly ICoordinateConverter coordinateConverter;

		public List<BranchViewModel> Branches { get; } = new List<BranchViewModel>();
		public List<MergeViewModel> Merges { get; } = new List<MergeViewModel>();
		public List<CommitViewModel> Commits { get; } = new List<CommitViewModel>();


		public RepositoryItems(ICoordinateConverter coordinateConverter)
		{
			this.coordinateConverter = coordinateConverter;
		}


		public Rect VirtualArea { get; set; } = Rect.Empty;

		public event EventHandler ItemsChanged;


		public void TriggerItemsChanged()
		{
			ItemsChanged?.Invoke(this, EventArgs.Empty);
		}


		/// <summary>
		/// Returns range of item ids, which are visible in the area currently shown
		/// </summary>
		public IEnumerable<int> GetItemIds(Rect viewArea)
		{
			if (VirtualArea == Rect.Empty || viewArea == Rect.Empty)
			{
				yield break;
			}

			// Get the part of the rectangle that is visible
			viewArea.Intersect(VirtualArea);

			int viewAreaTopIndex = coordinateConverter.GetTopRowIndex(viewArea, Commits.Count);
			int vareaBottomIndex = coordinateConverter.GetBottomRowIndex(viewArea, Commits.Count);

			if (vareaBottomIndex > viewAreaTopIndex)
			{
				// Return visible branches
				for (int i = 0; i < Branches.Count; i++)
				{
					BranchViewModel branch = Branches[i];
					if (IsVisable(
						viewAreaTopIndex, vareaBottomIndex, branch.LatestRowIndex, branch.FirstRowIndex))
					{
						yield return i + branchBaseIndex;
					}
				}

				// Return visible merges
				for (int i = 0; i < Merges.Count; i++)
				{
					MergeViewModel merge = Merges[i];
					if (IsVisable(
						viewAreaTopIndex, vareaBottomIndex, merge.ChildRowIndex, merge.ParentRowIndex))
					{
						yield return i + mergeBaseIndex;
					}
				}

				// Return visible commits
				for (int i = viewAreaTopIndex; i <= vareaBottomIndex; i++)
				{
					if (i >= 0 && i < Commits.Count)
					{
						yield return i;
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
		public object GetItem(int id)
		{
			if (Commits.Count == 0)
			{
				// No items yet
				return null;
			}

			if (id < branchBaseIndex)
			{
				if (Commits.Count > 0 && id >= 0 && id < Commits.Count)
				{
					return Commits[id];
				}
			}
			else if (id < mergeBaseIndex)
			{
				// An item in the branch range
				int branchId = id - branchBaseIndex;

				return Branches[branchId];
			}
			else
			{
				// An item in the merge range
				int mergeId = id - mergeBaseIndex;

				return Merges[mergeId];
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