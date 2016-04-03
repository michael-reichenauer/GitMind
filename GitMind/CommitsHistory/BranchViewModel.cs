using System.Windows;
using System.Windows.Media;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class BranchViewModel : ViewModel
	{
		public BranchViewModel(
			string name,
			int branchId,
			int latestRowIndex,
			int firstRowIndex,
			Rect rect,
			string line,
			Brush brush,
			string branchToolTip)
		{
			Name = name;
			BranchId = branchId;
			LatestRowIndex = latestRowIndex;
			FirstRowIndex = firstRowIndex;
			Rect = rect;
			Line = line;
			Brush = brush;
			BranchToolTip = branchToolTip;
			BranchColumn = branchId;
		}

		public string Type => "Branch";
		public string Name { get; }
		public int BranchId { get; }
		public int BranchColumn { get; }
		public int LatestRowIndex { get; }
		public int FirstRowIndex { get; }
		public Rect Rect { get; }
		public double Width => Rect.Width;
		public string Line { get; }
		public Brush Brush { get; }
		public string BranchToolTip { get; }

		public override string ToString() => $"{Name}";
	}
}