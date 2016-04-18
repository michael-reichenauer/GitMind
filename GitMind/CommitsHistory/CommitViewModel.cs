using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.DataModel.Private;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class CommitViewModel : ViewModel
	{
		private readonly Func<double> width;
		private readonly Func<string, Task> hideBranchAsync;
		private readonly Func<string, Task> showDiffAsync;


		public CommitViewModel(
			int itemId,
			Func<double> width,
			Func<string, Task> hideBranchAsync,
			Func<string, Task> showDiffAsync)
		{
			ItemId = itemId;
			this.width = width;
			this.hideBranchAsync = hideBranchAsync;
			this.showDiffAsync = showDiffAsync;
		}


		public int ItemId { get; }
		public Commit Commit { get; set; }
		public string Id => Commit.Id;
		public string Author => Commit.Author;
		public string Date { get; set; }

		public string Subject => Commit.Subject;
		public string Tags { get; set; }
		public string Tickets { get; set; }

		public bool IsCurrent
		{
			get { return Get(); }
			set { Set(value); }
		}

		// The branch point 
		public bool IsMergePoint { get; set; }
		public int BranchColumn { get; set; }

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

		public string Type => "Commit";

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


		public string ToolTip { get; set; }
		public Brush Brush { get; set; }

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
	
		public string CommitBranchText { get; set; }
		public string CommitBranchName { get; set; }

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


		private static string GetTagsText(Commit commit)
		{
			return commit.Tags.Count == 0
				? ""
				: "[" + string.Join("],[", commit.Tags.Select(t => t.Text)) + "] ";
		}



		private string GetTickets()
		{
			if (Commit.Subject.StartsWith("#"))
			{
				int index = Commit.Subject.IndexOf(" ");
				if (index > 1)
				{
					return Commit.Subject.Substring(0, index);
				}
				if (index > 0)
				{
					index = Commit.Subject.IndexOf(" ", index + 1);
					return Commit.Subject.Substring(0, index);
				}
			}

			return "";
		}


		private string GetSubjectWithouTags(string tags)
		{
			return Commit.Subject.Substring(tags.Length);
		}
	}
}