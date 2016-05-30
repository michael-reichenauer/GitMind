using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class BranchViewModel : ViewModel
	{
		public string Type => "Branch";


		public BranchViewModel(int branchColumn)
		{
			BranchColumn = branchColumn;
		}

		public Branch Branch { get; set; }

		public int BranchColumn { get; }

		public string Name
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int LatestRowIndex
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int FirstRowIndex
		{
			get { return Get(); }
			set { Set(value); }
		}

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

		public string Line
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Brush Brush
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string BranchToolTip
		{
			get { return Get(); }
			set { Set(value); }
		}



		public override string ToString() => $"{Name}";
	}
}