using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GitMind.Common.ThemeHandling;
using GitMind.Features.Branches;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class BranchViewModel : ViewModel
	{
		private readonly IBranchService branchService;
		private readonly IThemeService themeService;
		private readonly IRepositoryCommands repositoryCommands;

		private readonly Command<Branch> showBranchCommand;

		private readonly ObservableCollection<BranchItem> childBranches
			= new ObservableCollection<BranchItem>();

		private Commit mouseOnCommit = null;

		public BranchViewModel(
			IBranchService branchService,
			IThemeService themeService,
			IRepositoryCommands repositoryCommands)
		{
			this.branchService = branchService;
			this.themeService = themeService;
			this.repositoryCommands = repositoryCommands;
			this.showBranchCommand = Command<Branch>(repositoryCommands.ShowBranch);
		}

		// UI properties
		public string Type => nameof(BranchViewModel);
		public int ZIndex => 200;
		public string Id => Branch.Id;
		public string Name => Branch.Name;
		public bool IsMultiBranch => Branch.IsMultiBranch;
		public bool HasChildren => GetChildBranches().Any();
		public Rect Rect { get; set; }
		public double Width => Rect.Width;
		public double Top => Rect.Top;
		public double Left => Rect.Left;
		public double Height => Rect.Height;
		public string Line { get; set; }
		public string Dashes { get; set; }

		public int NeonEffect { get; set; }

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
			themeService.ChangeBranchBrush(Branch);
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
			NeonEffect = themeService.Theme.NeonEffect;
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
			List<Branch> branches;
			if (mouseOnCommit == null)
			{
				branches = Branch.GetChildBranches()
					.Where(b => !ActiveBranches.Any(ab => ab.Branch == b))
					.Take(19)
					.ToList();
			}
			else
			{
				branches = new List<Branch>();
				List<Branch> branchesBefore = new List<Branch>();
				List<Branch> branchesAfter = new List<Branch>();
				int total = 15;
				var commits = Branch.Commits.ToList();
				int index = commits.FindIndex(c => c == mouseOnCommit);
				if (index != -1)
				{
					int i1 = index;
					int i2 = index + 1;
					while (branchesBefore.Count + branchesAfter.Count < total && (i1 > -1 || i2 < commits.Count))
					{
						if (i1 > -1)
						{
							Commit commit = commits[i1];
							foreach (Commit child in commit.Children.Concat(commit.Parents).Where(c => c.Branch.Name != Branch.Name))
							{
								if (!branchesBefore.Any(b => b == child.Branch)
									&& !branchesAfter.Any(b => b == child.Branch
									&& branchesBefore.Count + branchesAfter.Count < total))
								{
									branchesBefore.Add(child.Branch);
								}
							}
						}

						if (i2 < commits.Count)
						{
							Commit commit = commits[i2];
							foreach (Commit child in commit.Children.Concat(commit.Parents).Where(c => c.Branch.Name != Branch.Name))
							{
								if (!branchesBefore.Any(b => b == child.Branch) 
									&& !branchesAfter.Any(b => b == child.Branch
									&& branchesBefore.Count + branchesAfter.Count < total))
								{
									branchesAfter.Add(child.Branch);
								}
							}
						}

						i1--;
						i2++;
					}

					branchesBefore.AsEnumerable().Reverse().ForEach(b => branches.Add(b));
					branchesAfter.ForEach(b => branches.Add(b));
				}
			}

			return BranchItem.GetBranches(branches, showBranchCommand);
		}


		public void SetColor(Brush brush)
		{
			Brush = brush;
			HoverBrushNormal = Brush;
			HoverBrushHighlight = themeService.Theme.GetLighterBrush(Brush);
			DimBrushHighlight = themeService.Theme.GetLighterLighterBrush(Brush);
		}


		public void MouseOnCommit(Commit commit)
		{
			mouseOnCommit = commit;

			Notify(nameof(ChildBranches), nameof(HasChildren));
		}
	}
}