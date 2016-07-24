using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitViewModel : ViewModel
	{
		private readonly ICommand refreshManuallyCommand;
		private readonly IDiffService diffService = new DiffService();
		private readonly IRepositoryService repositoryService = new RepositoryService();


		private int windowWidth;
		private static readonly SolidColorBrush HoverBrushColor =
			(SolidColorBrush)(new BrushConverter().ConvertFrom("#996495ED"));


		public CommitViewModel
			(ICommand refreshManuallyCommand,
			ICommand toggleDetailsCommand)
		{
			ToggleDetailsCommand = toggleDetailsCommand;
			this.refreshManuallyCommand = refreshManuallyCommand;
		}


		public Commit Commit { get; set; }

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
		public string CommitBranchText => "Hide branch: " + Commit.Branch.Name;
		public string CommitBranchName => Commit.Branch.Name;
		public bool IsMergePoint => Commit.IsMergePoint && Commit.Branch != Commit.SecondParent.Branch;
		public bool IsCurrent => Commit.IsCurrent;

		public Action HideBranch { get; set; }
		public int RowIndex { get; set; }

		public int BranchColumn { get; set; }
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
		public Brush HoverBrush => HoverBrushColor;


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


		public ICommand ToggleDetailsCommand { get; }

		public Command HideBranchCommand => Command(HideBranch);

		public Command ShowDiffCommand => Command(() => diffService.ShowDiffAsync(Id, Commit.WorkingFolder));

		public Command SetCommitBranchCommand => AsyncCommand(SetBranch);


		public void SetDim()
		{
			SubjectBrush = BrushService.DimBrush;
			TagBrush = BrushService.DimBrush;
			TicketBrush = BrushService.DimBrush;
			BranchTipBrush = BrushService.DimBrush;

			Notify(nameof(SubjectBrush), nameof(TicketBrush), nameof(TagBrush), nameof(BranchTipBrush));
		}


		public override string ToString() => $"{ShortId} {Subject} {Date}";


		public void SetNormal(Brush subjectBrush)
		{
			SubjectBrush = subjectBrush;
			TagBrush = BrushService.TagBrush;
			TicketBrush = BrushService.TicketBrush;
			BranchTipBrush = BrushService.BranchTipBrush;

			Notify(nameof(SubjectBrush), nameof(TicketBrush), nameof(TagBrush), nameof(BranchTipBrush));
		}


		private async Task SetBranch()
		{
			SetBranchPromptDialog dialog = new SetBranchPromptDialog();
			dialog.PromptText = Commit.SpecifiedBranchName;
			dialog.IsAutomatically = string.IsNullOrEmpty(Commit.SpecifiedBranchName);
			foreach (Branch childBranch in Commit.Branch.GetChildBranches())
			{
				if (!childBranch.IsMultiBranch && !childBranch.Name.StartsWith("_"))
				{
					dialog.AddBranchName(childBranch.Name);
				}
			}

			if (dialog.ShowDialog() == true)
			{
				Application.Current.MainWindow.Focus();
				string branchName = dialog.IsAutomatically ? null : dialog.PromptText?.Trim();
				string workingFolder = Commit.WorkingFolder;

				if (Commit.SpecifiedBranchName != branchName)
				{
					await repositoryService.SetSpecifiedCommitBranchAsync(Id, branchName, workingFolder);

					refreshManuallyCommand.Execute(null);
				}
			}
			else
			{
				Application.Current.MainWindow.Focus();
			}
		}
	}
}