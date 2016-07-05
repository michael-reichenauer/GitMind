using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class BranchViewModel : ViewModel, IVirtualItem
	{
		private readonly ICommand showBranchCommand;

		public string Type => "Branch";
		public int ZIndex => 200;

		public BranchViewModel(
			string id, 
			int virtualId,
			ICommand showBranchCommand,
			ICommand hideBranchCommand)
		{
			this.showBranchCommand = showBranchCommand;


			Id = id;
			VirtualId = virtualId;
			HideBranchCommand = hideBranchCommand;
		}


		public int VirtualId { get; }
		public ICommand HideBranchCommand { get; }
		public string Id { get; }

		public IReadOnlyList<BranchName> ChildBranches =>
			Branch.GetChildBranches().Take(50).Select(b => new BranchName(b, showBranchCommand)).ToList();		


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

		public Brush HoverBrush => Brush;

		public string BranchToolTip
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string HideBranchText => "Hide branch: " + Branch.Name;

		public override string ToString() => $"{Name}";
	}
}