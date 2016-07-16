using System;
using System.Collections.Generic;
using System.Windows;
using GitMind.Utils;
using GitMind.Utils.UI.VirtualCanvas;


namespace GitMind.CommitsHistory
{
	internal class RepositoryVirtualItemsSource : VirtualItemsSource
	{
		private readonly KeyedList<string, IVirtualItem> items =
			new KeyedList<string, IVirtualItem>(item => item.Id);
		
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


		public T GetOrAdd<T>(string id, Func<string, int, T> itemFactory) where T: IVirtualItem
		{
			IVirtualItem item;
			if (!items.TryGetValue(id, out item))
			{
				int virtualId = items.Count;
				item = itemFactory(id, virtualId);
				items.Add(item);
			}

			return (T)item;
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
				foreach (BranchViewModel branch in branches)
				{
					if (IsVisable(
						viewAreaTopIndex, viewAreaBottomIndex, branch.LatestRowIndex, branch.FirstRowIndex))
					{
						yield return branch.VirtualId;
					}
				}

				// Return visible merges
				foreach (MergeViewModel merge in merges)
				{
					if (IsVisable(viewAreaTopIndex, viewAreaBottomIndex, merge.ChildRow, merge.ParentRow))
					{
						yield return merge.VirtualId;
					}
				}

				// Return visible commits
				for (int i = viewAreaTopIndex; i <= viewAreaBottomIndex; i++)
				{
					if (i >= 0 && i < commits.Count)
					{
						var commit = commits[i];
						yield return commit.VirtualId;
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
			if (virtualId < items.Count)
			{
				return items[virtualId];
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