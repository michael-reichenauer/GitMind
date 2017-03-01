using System.Windows;
using System.Windows.Media;
using GitMind.Common.ThemeHandling;
using GitMind.Features.Branches;
using GitMind.Features.Commits;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitViewModel : ViewModel
	{
		private readonly IBranchService branchService;
		private readonly IThemeService themeService;
		private readonly IRepositoryCommands repositoryCommands;
		private readonly ICommitsService commitsService;

		private int windowWidth;


		public CommitViewModel(
			IBranchService branchService,
			IThemeService themeService,
			IRepositoryCommands repositoryCommands,
			ICommitsService commitsService)
		{
			this.branchService = branchService;
			this.themeService = themeService;
			this.repositoryCommands = repositoryCommands;
			this.commitsService = commitsService;
		}


		public int ZIndex => 400;
		public string Type => nameof(CommitViewModel);
		public string CommitId => Commit.RealCommitSha.Sha;
		public string ShortId => Commit.RealCommitSha.ShortSha;
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
		public bool IsUncommitted => Commit.IsUncommitted;
		public bool CanUncommit => UncommitCommand.CanExecute();
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
		public Brush TagBackgroundBrush { get; set; }
		public Brush TicketBrush { get; set; }
		public Brush TicketBackgroundBrush { get; set; }
		public Brush BranchTipBrush { get; set; }
		//public FontWeight SubjectWeight => Commit.CommitBranchName != null ? FontWeights.Bold : FontWeights.Normal;

		public string ToolTip { get; set; }
		public Brush Brush { get; set; }
		public FontStyle SubjectStyle => Commit.IsVirtual ? FontStyles.Italic : FontStyles.Normal;
		public Brush HoverBrush => themeService.Theme.HoverBrush;


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


		public Command ToggleDetailsCommand => Command(repositoryCommands.ToggleCommitDetails);
		public Command ShowCommitDiffCommand => Command(
			() => repositoryCommands.ShowDiff(
				Commit.IsVirtual && !Commit.IsUncommitted ? Commit.FirstParent : Commit));

		public Command SetCommitBranchCommand => Command(
			() => commitsService.EditCommitBranchAsync(Commit));
		public Command SwitchToCommitCommand => Command(
			() => branchService.SwitchToBranchCommitAsync(Commit),
			() => branchService.CanExecuteSwitchToBranchCommit(Commit));

		public Command SwitchToBranchCommand => Command(
			() => branchService.SwitchBranchAsync(Commit.Branch),
			() => branchService.CanExecuteSwitchBranch(Commit.Branch));

		public Command CreateBranchFromCommitCommand => Command(
			() => branchService.CreateBranchFromCommitAsync(Commit));

		public Command UndoUncommittedChangesCommand => AsyncCommand(
			() => commitsService.UndoUncommittedChangesAsync());

		public Command CleanWorkingFolderCommand => AsyncCommand(
			commitsService.CleanWorkingFolderAsync);

		public Command UncommitCommand => AsyncCommand(
		 () => commitsService.UnCommitAsync(Commit), () => commitsService.CanUnCommit(Commit));

		public Command UndoCommitCommand => AsyncCommand(
			() => commitsService.UndoCommitAsync(Commit), () => commitsService.CanUndoCommit(Commit));



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
			SubjectBrush = themeService.Theme.DimBrush;
			TagBrush = themeService.Theme.DimBrush;
			TicketBackgroundBrush = themeService.Theme.BackgroundBrush;
			TicketBrush = themeService.Theme.DimBrush;
			TicketBackgroundBrush = themeService.Theme.BackgroundBrush;
			BranchTipBrush = themeService.Theme.DimBrush;

			Notify(nameof(SubjectBrush), nameof(TicketBrush), nameof(TagBrush), nameof(BranchTipBrush));
		}


		public void SetNormal(Brush subjectBrush)
		{
			SubjectBrush = subjectBrush;
			TagBrush = themeService.Theme.TagBrush;
			TagBackgroundBrush = themeService.Theme.TagBackgroundBrush;
			TicketBrush = themeService.Theme.TicketBrush;
			TicketBackgroundBrush = themeService.Theme.TicketBackgroundBrush;
			BranchTipBrush = themeService.Theme.BranchTipsBrush;

			Notify(nameof(SubjectBrush), nameof(TicketBrush), nameof(TagBrush), nameof(BranchTipBrush));
		}


		public override string ToString() => $"{ShortId} {Subject} {Date}";
	}
}