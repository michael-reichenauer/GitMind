using System;
using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitViewModel : ViewModel, IVirtualItem
	{
		private readonly IDiffService diffService = new DiffService();
		private readonly IRepositoryService repositoryService = new RepositoryService();

		private Commit commit;
		private int windowWidth;
		private static readonly SolidColorBrush HoverBrushColor = 
			(SolidColorBrush)(new BrushConverter().ConvertFrom("#996495ED"));


		public CommitViewModel(
			string id,
			int virtualId)
		{
			Id = id;
			VirtualId = virtualId;
		}



		public int ZIndex => 0;
		public string Id { get; }
		public int VirtualId { get; }

		public string Type => "Commit";

		public Action HideBranch { get; set; }

		public Commit Commit
		{
			get { return commit; }
			set
			{
				if (commit != value)
				{
					commit = value;
					Notify(nameof(Id), nameof(ShortId), nameof(Author), nameof(Date), nameof(Subject));
				}
			}
		}

		public int RowIndex { get; set; }


		public string ShortId => Commit.ShortId;
		public string Author => Commit.Author;
		public string Date => Commit.AuthorDateText;
		public string Subject => Commit.Subject;

		public string Tags
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Tickets
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string BranchTips
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool IsCurrent
		{
			get { return Get(); }
			set { Set(value); }
		}

		// The branch point 
		public bool IsMergePoint
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int BranchColumn
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int XPoint
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int YPoint
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int Size
		{
			get { return Get(); }
			set { Set(value); }
		}

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

		public Brush SubjectBrush
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string ToolTip
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Brush Brush
		{
			get { return Get(); }
			set { Set(value); }
		}

		public FontStyle SubjectStyle
		{
			get { return Get<FontStyle>(); }
			set { Set<FontStyle>(value); }
		}

		public Brush HoverBrush => HoverBrushColor;

		public Brush BrushInner
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Rect Rect
		{
			get { return Get(); }
			set { Set(value); }
		}


		public string CommitBranchText
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string CommitBranchName
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