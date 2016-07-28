using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class BranchViewModel : ViewModel
	{
		private readonly Command<Branch> showBranchCommand;


		public BranchViewModel(Command<Branch> showBranchCommand)
		{
			this.showBranchCommand = showBranchCommand;
		}


		public string Type => nameof(BranchViewModel);

		public int ZIndex => 200;


		public ObservableCollection<BranchItem> ActiveBranches { get; set; }
	

		public Branch Branch { get; set; }


		public IReadOnlyList<BranchItem> ChildBranches =>
			BranchItem.GetBranches(
				Branch.GetChildBranches()
					.Where(b => !ActiveBranches.Any(ab => ab.Branch == b))
					.Take(50)
					.ToList(),
				showBranchCommand);

		public IReadOnlyList<BranchNameItem> MultiBranches { get; set; }

		public string Id => Branch.Id;
		public string Name => Branch.Name;
		public bool IsMultiBranch => Branch.IsMultiBranch;
		public bool HasChildren => ChildBranches.Count > 0;

		public int BranchColumn { get; set; }
		public int LatestRowIndex { get; set; }
		public int FirstRowIndex { get; set; }
		public Rect Rect { get; set; }


		public double Width => Rect.Width;
		public string Line { get; set; }
		public int StrokeThickness { get; set; }
		public Brush Brush { get; set; }

		public Brush HoverBrush { get; set; }
		public Brush HoverBrushNormal { get; set; }
		public Brush HoverBrushHighlight { get; set; }


		public string BranchToolTip { get; set; }
		public string HideBranchText => "Hide branch: " + Branch.Name;
		public int Height { get; set; }


		public override string ToString() => $"{Name}";


		public void SetNormal()
		{
			StrokeThickness = 2;
			Brush = HoverBrushNormal;

			Notify(nameof(StrokeThickness), nameof(Brush));
		}


		public void SetHighlighted()
		{
			StrokeThickness = 3;
			Brush = HoverBrushHighlight;
			Notify(nameof(StrokeThickness), nameof(Brush));
		}
	}
}