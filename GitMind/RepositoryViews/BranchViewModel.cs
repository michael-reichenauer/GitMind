using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GitMind.Features.Branching;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class BranchViewModel : ViewModel
	{
		private readonly IBranchService branchService = new BranchService();
		private readonly IRepositoryCommands repositoryCommands;
		private readonly Command<Branch> showBranchCommand;
		private readonly Command<Branch> deleteLocalBranchCommand;
		private readonly Command<Branch> deleteRemoteBranchCommand;

		private readonly ObservableCollection<BranchItem> childBranches 
			= new ObservableCollection<BranchItem>();

		public BranchViewModel(
			IRepositoryCommands repositoryCommands,
			Command<Branch> showBranchCommand,
			Command<Branch> mergeBranchCommand,
			Command<Branch> deleteLocalBranchCommand,
			Command<Branch> deleteRemoteBranchCommand)
		{
			this.repositoryCommands = repositoryCommands;
			this.showBranchCommand = showBranchCommand;
			this.deleteLocalBranchCommand = deleteLocalBranchCommand;
			this.deleteRemoteBranchCommand = deleteRemoteBranchCommand;

			MergeBranchCommand = mergeBranchCommand.With(() => Branch);
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
		public int StrokeThickness { get; set; }
		public Brush Brush { get; set; }
		public Brush HoverBrush { get; set; }
		public Brush HoverBrushNormal { get; set; }
		public Brush HoverBrushHighlight { get; set; }
		public string BranchToolTip { get; set; }
		public bool IsMergeable => Branch.IsMergeable;
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
			() => branchService.SwitchBranchAsync(repositoryCommands, Branch),
			() => branchService.CanExecuteSwitchBranch(Branch));

		public Command CreateBranchCommand => Command(
			() => branchService.CreateBranchAsync(repositoryCommands, Branch));

		public Command MergeBranchCommand { get; }
		public Command DeleteLocalBranchCommand => 
			Command(() => deleteLocalBranchCommand.Execute(Branch), () => Branch.IsLocal);
		public Command DeleteRemoteBranchCommand =>
			Command(() => deleteRemoteBranchCommand.Execute(Branch), () => Branch.IsRemote);


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
				showBranchCommand);
		}
	}
}