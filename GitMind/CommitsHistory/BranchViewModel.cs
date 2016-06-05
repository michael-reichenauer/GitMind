using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class BranchViewModel : ViewModel, IVirtualItem
	{
		public string Type => "Branch";


		public BranchViewModel(string id, int virtualId)
		{
			Id = id;
			VirtualId = virtualId;
		}


		public int VirtualId { get; }
		public string Id { get; } 

		public Branch Branch { get; set; }

		public int BranchColumn { get; set; }

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