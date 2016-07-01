using System.Windows;
using System.Windows.Media;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class OldBranchViewModel : ViewModel
	{
		public OldBranchViewModel(
			string name,
			int branchId,
			int branchColumn,
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
			Width =rect.Width;
			Line = line;
			Brush = brush;
			BranchToolTip = branchToolTip;
			BranchColumn = branchColumn;
		}

		public string Type => "Branch";
		public string Name { get; }
		public int BranchId { get; }
		public int BranchColumn { get; }
		public int LatestRowIndex { get; }
		public int FirstRowIndex { get; }

		public Rect Rect
		{
			get { return Get(); }
			set { Set(value); }
		}

		public double Width
		{
			get { return Get(); }
			set { Set(value); }
		}
		public string Line { get; }
		public Brush Brush { get; }
		public string BranchToolTip { get; }

		public override string ToString() => $"{Name}";
	}
}