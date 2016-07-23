using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class BranchViewModel : ViewModel
	{
		private readonly ICommand showBranchCommand;

		public string Type => nameof(BranchViewModel);
		public int ZIndex => 200;

		public BranchViewModel(ICommand showBranchCommand, ICommand hideBranchCommand)
		{			
			this.showBranchCommand = showBranchCommand;
			HideBranchCommand = hideBranchCommand;
		}

		public ObservableCollection<BranchItem> ActiveBranches { get; set; }
		public ICommand HideBranchCommand { get; }

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
		public Brush Brush { get; set; }	
		public Brush HoverBrush { get; set; }

		public string BranchToolTip { get; set; }
		public string HideBranchText => "Hide branch: " + Branch.Name;

		public override string ToString() => $"{Name}";
	}
}