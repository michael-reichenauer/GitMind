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
		private readonly Command<Branch> mergeBranchCommand;


		public BranchViewModel(
			Command<Branch> showBranchCommand,
			Command<Branch> switchBranchCommand,
			Command<Branch> mergeBranchCommand,
			Command<Branch> createBranchCommand)
		{			
			this.showBranchCommand = showBranchCommand;
			this.mergeBranchCommand = mergeBranchCommand;

			SwitchBranchCommand = switchBranchCommand.With(() => Branch);
			CreateBranchCommand = createBranchCommand.With(() => Branch);
		}

		// UI properties
		public string Type => nameof(BranchViewModel);
		public int ZIndex => 200;
		public string Id => Branch.Id;
		public string Name => Branch.Name;
		public bool IsMultiBranch => Branch.IsMultiBranch;
		public bool HasChildren => ChildBranches.Count > 0;
		public Rect Rect { get; set; }
		public double Width => Rect.Width;
		public string Line { get; set; }
		public int StrokeThickness { get; set; }
		public Brush Brush { get; set; }
		public Brush HoverBrush { get; set; }
		public Brush HoverBrushNormal { get; set; }
		public Brush HoverBrushHighlight { get; set; }
		public string BranchToolTip { get; set; }
		public bool IsMergeable => Branch.IsMergeable;
		public string SwitchBranchText => $"Switch to branch '{Name}'";
		public string MergeBranchText => $"Merge branch into '{Name}' from";

		// Values used by UI properties
		public Branch Branch { get; set; }
		public ObservableCollection<BranchItem> ActiveBranches { get; set; }
		public ObservableCollection<BranchItem> ShownBranches { get; set; }
		public IReadOnlyList<BranchItem> ChildBranches => GetChildBranches();
		public IReadOnlyList<BranchItem> OtherShownBranches => GetOtherChownBranches();



		public Command SwitchBranchCommand { get; }
		public Command CreateBranchCommand { get; }

		// Some values used by Merge items and to determine if item is visible
		public int BranchColumn { get; set; }
		public int TipRowIndex { get; set; }
		public int FirstRowIndex { get; set; }


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

		public override string ToString() => $"{Name}";


		private IReadOnlyList<BranchItem> GetChildBranches()
		{
			return BranchItem.GetBranches(
				Branch.GetChildBranches()
					.Where(b => !ActiveBranches.Any(ab => ab.Branch == b))
					.Take(50)
					.ToList(),
				showBranchCommand,
				mergeBranchCommand);
		}


		private IReadOnlyList<BranchItem> GetOtherChownBranches()
		{
			return BranchItem.GetBranches(
				ShownBranches
					.Where(b => b.Branch != Branch)
					.Select(b => b.Branch)
				.ToList(),
				showBranchCommand,
				mergeBranchCommand);
		}

	}
}