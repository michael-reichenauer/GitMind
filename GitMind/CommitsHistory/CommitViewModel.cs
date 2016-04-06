using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
			int branchColumn,
			Commit commit,
			bool isMergePoint,
			bool isCurrent,
			Rect rect,
			Func<double> width,
			int graphWidth,
			Brush subjectBrush,
			string date,
			string toolTip,
			Brush brush,
			Brush brushInner,
			int xPoint,
			int yPoint,
			int size,
			string commitBranchText,
			string commitBranchName,
			Func<string, Task> hideBranchAsync,
			Func<string, Task> showDiffAsync)
		{
			this.width = width;
			this.hideBranchAsync = hideBranchAsync;
			this.showDiffAsync = showDiffAsync;
			BranchColumn = branchColumn;
			Commit = commit;
			IsMergePoint = isMergePoint;
			IsCurrent = isCurrent;
			Rect = rect;
			
			GraphWidth = graphWidth;
			SubjectBrush = subjectBrush;
			Date = date;
			ToolTip = toolTip;
			Brush = brush;
			BrushInner = brushInner;
			XPoint = xPoint;
			YPoint = yPoint;
			Size = size;
			CommitBranchText = commitBranchText;
			CommitBranchName = commitBranchName;
			CommitBranchName = commit.Branch.Name;
			Tags = GetTagsText(commit);
			Tickets = GetTickets();
			Subject = GetSubjectWithouTags(Tickets);
		}





		public Commit Commit { get; }
		public string Id => Commit.Id;
		public string Author => Commit.Author;
		public string Subject { get; }
		public DateTime DateTime => Commit.DateTime;
		public bool IsMergePoint { get; }
		public string Tags { get; }
		public int BranchColumn { get; }

		public string Tickets { get; }

		public bool IsCurrent { get; }


		public string Type => "Commit";

		public Rect Rect { get; }
		public double Width => width();
		public int GraphWidth { get; }
		public Brush SubjectBrush { get; }

		public string Date { get; }
		public string ToolTip { get; }
		public Brush Brush { get; set; }
		public Brush BrushInner { get; }
		public int XPoint { get; }
		public int YPoint { get; }
		public int Size { get; }

		public string CommitBranchText { get; }
		public string CommitBranchName { get; }

		public ICommand HideBranchCommand => Command(HideBranchAsync);
		public ICommand ShowDiffCommand => Command(ShowDiffAsync);


		public override string ToString() => $"{Commit.ShortId} {Subject} {DateTime}";


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