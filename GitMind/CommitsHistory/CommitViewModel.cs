using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class CommitViewModel : ViewModel
	{
		private readonly Func<string, Task> hideBranchAsync;
		private readonly Func<string, Task> showDiffAsync;
		private Commit commit;
		private int windowWidth;

		public CommitViewModel(
			int rowIndex,
			Func<string, Task> hideBranchAsync,
			Func<string, Task> showDiffAsync)
		{
			RowIndex = rowIndex;
			this.hideBranchAsync = hideBranchAsync;
			this.showDiffAsync = showDiffAsync;
		}

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

		public int RowIndex { get; }

		public string Id => Commit.Id;
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
			private set { Set(value); }
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
			private set { Set(value); }
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

		public Command HideBranchCommand => Command(HideBranchAsync);
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
					Rect = new Rect(0, Converter.ToY(RowIndex), Width, Converter.ToY(1));
					
				}
			}
		}


		public override string ToString() => $"{ShortId} {Subject} {Date}";


		private async void HideBranchAsync()
		{
			await hideBranchAsync(CommitBranchName);
		}


		private async void ShowDiffAsync()
		{
			await showDiffAsync(Id);
		}
	}
}