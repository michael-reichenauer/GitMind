using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitViewModel : ViewModel
	{
		private int windowWidth;


		public CommitViewModel(
			Command toggleDetailsCommand,
			Command<Commit> showCommitDiffCommand,
			Command<Commit> setBranchCommand,
			Command<Commit> switchToCommitCommand,
			Command<Branch> switchToBranchCommand,
			Command<Commit> createBranchFromCommitCommand)
		{
			ToggleDetailsCommand = toggleDetailsCommand;
			SetCommitBranchCommand = setBranchCommand.With(() => Commit);
			ShowCommitDiffCommand = showCommitDiffCommand.With(() => Commit);
			SwitchToCommitCommand = switchToCommitCommand.With(() => Commit);
			SwitchToBranchCommand = switchToBranchCommand.With(() => Commit.Branch);
			CreateBranchFromCommitCommand = createBranchFromCommitCommand.With(() => Commit);
		}


		public int ZIndex => 0;
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

		public int XPoint { get; set; }
		public int YPoint => IsMergePoint ? 2 : 4;
		public int Size => IsMergePoint ? 10 : 6;
		public Rect Rect { get; set; }
		public Brush SubjectBrush { get; set; }
		public Brush TagBrush { get; set; }
		public Brush TicketBrush { get; set; }
		public Brush BranchTipBrush { get; set; }
		public FontWeight SubjectWeight { get; set; }

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
		public Command SwitchToCommitCommand { get; }
		public Command SwitchToBranchCommand { get; }
		public Command CreateBranchFromCommitCommand { get; }

		// Values used by other properties
		public Commit Commit { get; set; }
		public bool IsMergePoint => 
			(Commit.IsMergePoint && Commit.Branch != Commit.SecondParent.Branch)
			|| (Commit.HasFirstParent && Commit.Branch != Commit.FirstParent.Branch);

		// Value used by merge and that determine if item is visible
		public int BranchColumn { get; set; }
		public int RowIndex { get; set; }


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