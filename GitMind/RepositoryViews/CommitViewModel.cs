using System.Windows;
using System.Windows.Media;
using GitMind.Features.Branching;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitViewModel : ViewModel
	{
		private readonly IBranchService branchService;
		private readonly IRepositoryCommands repositoryCommands;

		private int windowWidth;


		public CommitViewModel(
			IBranchService branchService,
			IRepositoryCommands repositoryCommands)
		{
			this.branchService = branchService;
			this.repositoryCommands = repositoryCommands;
			ToggleDetailsCommand = repositoryCommands.ToggleDetailsCommand;
			SetCommitBranchCommand = repositoryCommands.SetBranchCommand.With(() => Commit);
			ShowCommitDiffCommand = repositoryCommands.ShowDiffCommand.With(
				() => Commit.IsVirtual && !Commit.IsUncommitted ? Commit.FirstParent : Commit);
			UndoUncommittedChangesCommand = repositoryCommands.UndoUncommittedChangesCommand;
			UndoCleanWorkingFolderCommand = repositoryCommands.UndoCleanWorkingFolderCommand;
			UncommitCommand = repositoryCommands.UncommitCommand.With(() => Commit); ;
		}


		public int ZIndex => 400;
		public string Type => nameof(CommitViewModel);
		public string Id => Commit.Id;
		public string ShortId => Commit.ShortId;
		public string Author => Commit.Author;
		public string Date => Commit.AuthorDateText;
		public string Subject => Commit.Subject;
		public string Tags => Commit.Tags;
		public string Tickets => Commit.Tickets;
		public string BranchTips => Commit.BranchTips;
		public string CommitBranchText => $"Hide branch: {Commit.Branch.Name}";
		public string SwitchToBranchText => $"Switch to branch: {Commit.Branch.Name}";
		public string CommitBranchName => Commit.Branch.Name;
		public bool IsCurrent => Commit.IsCurrent;
		public bool IsUncommitted => Commit.Id == Commit.UncommittedId;
		public bool CanUncommit => !IsUncommitted && IsCurrent && Commit.IsLocalAhead;
		public bool IsShown => BranchTips == null;
		public string BranchToolTip { get; set; }

		public int XPoint { get; set; }
		public int YPoint => IsEndPoint ? 4 : IsMergePoint ? 2 : 4;
		public int Size => IsEndPoint ? 8 : IsMergePoint ? 10 : 6;
		public Rect Rect { get; set; }
		public double Top => Rect.Top;
		public double Left => Rect.Left;
		public double Height => Rect.Height;
		public Brush SubjectBrush { get; set; }
		public Brush TagBrush { get; set; }
		public Brush TicketBrush { get; set; }
		public Brush BranchTipBrush { get; set; }
		//public FontWeight SubjectWeight => Commit.CommitBranchName != null ? FontWeights.Bold : FontWeights.Normal;

		public string ToolTip { get; set; }
		public Brush Brush { get; set; }
		public FontStyle SubjectStyle => Commit.IsVirtual ? FontStyles.Italic : FontStyles.Normal;
		public Brush HoverBrush => BrushService.HoverBrushColor;


		public double Width
		{
			get { return Get(); }
			set { Set(value - 2); }
		}

		public int GraphWidth
		{
			get { return Get(); }
			set { Set(value); }
		}


		public Brush BrushInner
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int WindowWidth
		{
			get { return windowWidth; }
			set
			{
				if (windowWidth != value)
				{
					windowWidth = value;
					Width = windowWidth - 35;
				}
			}
		}


		public Command ToggleDetailsCommand { get; }
		public Command ShowCommitDiffCommand { get; }
		public Command SetCommitBranchCommand { get; }
		public Command SwitchToCommitCommand => Command(
			() => branchService.SwitchToBranchCommitAsync(repositoryCommands, Commit),
			() => branchService.CanExecuteSwitchToBranchCommit(Commit));

		public Command SwitchToBranchCommand => Command(
			() => branchService.SwitchBranchAsync(repositoryCommands, Commit.Branch),
			() => branchService.CanExecuteSwitchBranch(Commit.Branch));

		public Command CreateBranchFromCommitCommand => Command(
			() => branchService.CreateBranchFromCommitAsync(repositoryCommands, Commit));

		public Command UndoUncommittedChangesCommand { get; }
		public Command UndoCleanWorkingFolderCommand { get; }
		public Command UncommitCommand { get; }



		// Values used by other properties
		public Commit Commit { get; set; }

		// If second parent is other branch (i.e. no a pull merge)
		// If commit is first commit in a branch (first parent is other branch)
		// If commit is tip commit, but not master
		public bool IsMergePoint =>
			(Commit.IsMergePoint && Commit.Branch != Commit.SecondParent.Branch)
			|| (Commit.HasFirstParent && Commit.Branch != Commit.FirstParent.Branch)
			|| (Commit == Commit.Branch.TipCommit && Commit.Branch.Name != BranchName.Master);

		public bool IsEndPoint =>
			(Commit.HasFirstParent && Commit.Branch != Commit.FirstParent.Branch)
			|| (Commit == Commit.Branch.TipCommit && Commit.Branch.Name != BranchName.Master);

		// Value used by merge and that determine if item is visible
		public BranchViewModel BranchViewModel { get; set; }
		public int RowIndex { get; set; }
		public int X => BranchViewModel?.X ?? -20;
		public int Y => Converters.ToY(RowIndex) + 10;


		public void SetDim()
		{
			SubjectBrush = BrushService.DimBrush;
			TagBrush = BrushService.DimBrush;
			TicketBrush = BrushService.DimBrush;
			BranchTipBrush = BrushService.DimBrush;

			Notify(nameof(SubjectBrush), nameof(TicketBrush), nameof(TagBrush), nameof(BranchTipBrush));
		}


		public void SetNormal(Brush subjectBrush)
		{
			SubjectBrush = subjectBrush;
			TagBrush = BrushService.TagBrush;
			TicketBrush = BrushService.TicketBrush;
			BranchTipBrush = BrushService.BranchTipBrush;

			Notify(nameof(SubjectBrush), nameof(TicketBrush), nameof(TagBrush), nameof(BranchTipBrush));
		}


		public override string ToString() => $"{ShortId} {Subject} {Date}";
	}
}