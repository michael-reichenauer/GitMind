using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class CommitViewModel : ViewModel, IVirtualItem
	{
		private readonly IDiffService diffService = new DiffService();

		private Commit commit;
		private int windowWidth;

		public CommitViewModel(
			string id, 
			int virtualId)
		{
			Id = id;
			VirtualId = virtualId;
		}

		public string Id { get; }
		public int VirtualId { get; }

		public string Type => "Commit";

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
		//public string Subject => Commit.ShortId + " " + Commit.Subject;
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

		public int ZIndex => 0;
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

		public Command HideBranchCommand => Command(HideBranch);
		public Command ShowDiffCommand => Command(ShowDiffAsync);

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



		public override string ToString() => $"{ShortId} {Subject} {Date}";


		public Action HideBranch { get; set; }
		

		private async void ShowDiffAsync()
		{
			await diffService.ShowDiffAsync(Id);
		}
	}
}