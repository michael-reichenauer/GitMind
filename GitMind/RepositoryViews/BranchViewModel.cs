using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GitMind.Common.Brushes;
using GitMind.Features.Branches;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class BranchViewModel : ViewModel
	{
		private readonly IBranchService branchService;
		private readonly IBrushService brushService;
		private readonly IRepositoryCommands repositoryCommands;

		private readonly Command<Branch> showBranchCommand;

		private readonly ObservableCollection<BranchItem> childBranches
			= new ObservableCollection<BranchItem>();


		public BranchViewModel(
			IBranchService branchService,
			IBrushService brushService,
			IRepositoryCommands repositoryCommands)
		{
			this.branchService = branchService;
			this.brushService = brushService;
			this.repositoryCommands = repositoryCommands;
			this.showBranchCommand = Command<Branch>(repositoryCommands.ShowBranch);
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
		public double Top => Rect.Top;
		public double Left => Rect.Left;
		public double Height => Rect.Height;
		public string Line { get; set; }
		public string Dashes { get; set; }

		public int StrokeThickness { get; set; }
		public Brush Brush { get; set; }
		public Brush HoverBrush { get; set; }
		public Brush HoverBrushNormal { get; set; }
		public Brush HoverBrushHighlight { get; set; }
		public Color DimColor { get; set; }
		public string BranchToolTip { get; set; }
		public bool CanPublish => Branch.CanBePublish;
		public bool CanPush => Branch.CanBePushed;
		public bool CanUpdate => Branch.CanBeUpdated && !Branch.IsLocalPart;
		public bool IsMergeable => Branch.IsCanBeMergeToOther;


		public string SwitchBranchText => $"Switch to branch '{Name}'";
		public string MergeToBranchText => $"Merge to branch '{CurrentBranchName}'";
		public string CurrentBranchName { get; set; }
		public bool CanDeleteBranch => Branch.IsLocal || Branch.IsRemote;

		// Values used by UI properties
		public Branch Branch { get; set; }
		public ObservableCollection<BranchItem> ActiveBranches { get; set; }
		public ObservableCollection<BranchItem> ShownBranches { get; set; }

		public ObservableCollection<BranchItem> ChildBranches
		{
			get
			{
				childBranches.Clear();
				GetChildBranches().ForEach(b => childBranches.Add(b));
				return childBranches;
			}
		}

		public Command SwitchBranchCommand => Command(
			() => branchService.SwitchBranchAsync(Branch),
			() => branchService.CanExecuteSwitchBranch(Branch));

		public Command CreateBranchCommand => Command(
			() => branchService.CreateBranchAsync(Branch));

		public Command MergeBranchCommand => AsyncCommand(() => branchService.MergeBranchAsync(Branch));
		public Command DeleteBranchCommand => AsyncCommand(
			() => branchService.DeleteBranchAsync(Branch), () => branchService.CanDeleteBranch(Branch));

		public Command PublishBranchCommand => Command(() => branchService.PublishBranchAsync(Branch));

		public Command PushBranchCommand => Command(() => branchService.PushBranchAsync(Branch));

		public Command UpdateBranchCommand => Command(() => branchService.UpdateBranchAsync(Branch));

		public Command ChangeColorCommand => Command(() =>
		{
			brushService.ChangeBranchBrush(Branch);
			repositoryCommands.RefreshView();
		});

		// Some values used by Merge items and to determine if item is visible
		public int BranchColumn { get; set; }
		public int X { get; set; }
		public int TipRowIndex { get; set; }
		public int FirstRowIndex { get; set; }
		public Brush DimBrushHighlight { get; set; }


		public void SetNormal()
		{
			StrokeThickness = 2;
			Brush = HoverBrushNormal;
			DimColor = ((SolidColorBrush)HoverBrushHighlight).Color;

			Notify(nameof(StrokeThickness), nameof(Brush), nameof(DimColor));
		}


		public void SetHighlighted()
		{
			StrokeThickness = 3;
			Brush = HoverBrushHighlight;
			DimColor = ((SolidColorBrush)DimBrushHighlight).Color;
			Notify(nameof(StrokeThickness), nameof(Brush), nameof(DimColor));
		}

		public override string ToString() => $"{Branch}";


		private IReadOnlyList<BranchItem> GetChildBranches()
		{
			return BranchItem.GetBranches(
				Branch.GetChildBranches()
					.Where(b => !ActiveBranches.Any(ab => ab.Branch == b))
					.Take(50)
					.ToList(),
				showBranchCommand);
		}


		public void SetColor(Brush brush)
		{
			Brush = brush;
			HoverBrushNormal = Brush;
			HoverBrushHighlight = brushService.GetLighterBrush(Brush);
			DimBrushHighlight = brushService.GetLighterLighterBrush(Brush);
		}
	}
}