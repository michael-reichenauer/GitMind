using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.DataModel.Old;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class CommitViewModel : ViewModel
	{
		private readonly Func<string, Task> hideBranchAsync;
		private readonly Func<string, Task> showDiffAsync;


		public CommitViewModel(
			Func<string, Task> hideBranchAsync,
			Func<string, Task> showDiffAsync)
		{
			this.hideBranchAsync = hideBranchAsync;
			this.showDiffAsync = showDiffAsync;
		}

		public OldCommit Commit { get; set; }
		public string Type => "Commit";

		public string Id
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Author
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Date
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Subject
		{
			get { return Get(); }
			set { Set(value); }
		}

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
			set { Set(value); }
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


		public override string ToString() => $"{Commit.ShortId} {Subject} {Date}";


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