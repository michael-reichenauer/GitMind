using System;
using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitViewModel : ViewModel
	{
		private readonly IDiffService diffService = new DiffService();
		private readonly IRepositoryService repositoryService = new RepositoryService();

	
		private int windowWidth;
		private static readonly SolidColorBrush HoverBrushColor = 
			(SolidColorBrush)(new BrushConverter().ConvertFrom("#996495ED"));

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


		public Command HideBranchCommand => Command(HideBranch);

		public Command ShowDiffCommand => Command(() => diffService.ShowDiffAsync(Id, Commit.GitRepositoryPath));

		public Command SetCommitBranchCommand => Command(async () =>
		{
			var dialog = new SetBranchPrompt();
			dialog.PromptText = Commit.SpecifiedBranchName;

			if (dialog.ShowDialog() == true)
			{
				string branchName = dialog.PromptText?.Trim();
				string gitRepositoryPath = Commit.GitRepositoryPath;
				await repositoryService.SetSpecifiedCommitBranchAsync(Id, branchName, gitRepositoryPath);
			}
		});



		public override string ToString() => $"{ShortId} {Subject} {Date}";		
	}
}