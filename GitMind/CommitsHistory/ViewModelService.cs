using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using GitMind.DataModel.Old;
using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal class ViewModelService
	{
		private readonly ICoordinateConverter coordinateConverter;


		public ViewModelService(ICoordinateConverter coordinateConverter)
		{
			this.coordinateConverter = coordinateConverter;
		}


		public RepositoryViewModel GetRepositoryViewModel(Repository repository)
		{

			return null;
		}


		private void CreateRows()
		{
			int graphWidth = coordinateConverter.ConvertFromColumn(model.Branches.Count);

			IReadOnlyList<OldCommit> sourceCommits = model.Commits;

			//if (!string.IsNullOrWhiteSpace(filterText))
			//{
			//	sourceCommits = model.GitRepo.GetAllCommts()
			//		.Where(c => c.Subject.IndexOf(filterText, StringComparison.CurrentCultureIgnoreCase) != -1
			//		|| c.Author.IndexOf(filterText, StringComparison.CurrentCultureIgnoreCase) != -1
			//		|| c.Id.StartsWith(filterText, StringComparison.CurrentCultureIgnoreCase))
			//		.Select(c => model.GetCommit(c.Id))
			//		.ToList();
			//}


			int commitsCount = sourceCommits.Count;
			SetNumberOfCommit(commitsCount);

			for (int rowIndex = 0; rowIndex < commitsCount; rowIndex++)
			{
				OldCommit commit = sourceCommits[rowIndex];

				OldCommitViewModel commitViewModel = commits[rowIndex];

				commitViewModel.Commit = commit;
				commitViewModel.Id = commit.Id;
				commitViewModel.Rect = new Rect(
					0,
					coordinateConverter.ConvertFromRow(rowIndex),
					Width - 35,
					coordinateConverter.ConvertFromRow(1));

				commitViewModel.IsCurrent = commit == model.CurrentCommit;

				if (string.IsNullOrWhiteSpace(filterText))
				{
					commitViewModel.IsMergePoint = commit.Parents.Count > 1
						&& (!commit.SecondParent.IsOnActiveBranch()
						|| commit.Branch != commit.SecondParent.Branch);

					commitViewModel.BranchColumn = GetBranchColumnForBranchName(commit.Branch.Name);

					commitViewModel.Size = commitViewModel.IsMergePoint ? 10 : 6;
					commitViewModel.XPoint = commitViewModel.IsMergePoint
						? 2 + coordinateConverter.ConvertFromColumn(commitViewModel.BranchColumn)
						: 4 + coordinateConverter.ConvertFromColumn(commitViewModel.BranchColumn);
					commitViewModel.YPoint = commitViewModel.IsMergePoint ? 2 : 4;

					commitViewModel.Brush = brushService.GetBRanchBrush(commit.Branch);
					commitViewModel.BrushInner = commit.IsExpanded
						? brushService.GetDarkerBrush(commitViewModel.Brush)
						: commitViewModel.Brush;

					commitViewModel.CommitBranchText = "Hide branch: " + commit.Branch.Name;
					commitViewModel.CommitBranchName = commit.Branch.Name;
					commitViewModel.ToolTip = GetCommitToolTip(commit);
					commitViewModel.SubjectBrush = GetSubjectBrush(commit);
				}
				else
				{
					commitViewModel.SubjectBrush = brushService.SubjectBrush;
					commitViewModel.IsMergePoint = false;
					commitViewModel.BranchColumn = 0;
					commitViewModel.Size = 0;
					commitViewModel.XPoint = 0;
					commitViewModel.YPoint = 0;
					commitViewModel.Brush = Brushes.Black;
					commitViewModel.BrushInner = Brushes.Black;
					commitViewModel.CommitBranchText = "";
					commitViewModel.CommitBranchName = "";
					commitViewModel.ToolTip = "";
				}

				commitViewModel.GraphWidth = graphWidth;


				commitViewModel.Width = Width - 35;

				commitViewModel.Date = GetCommitDate(commit);
				commitViewModel.Author = commit.Author;
				commitViewModel.Subject = GetSubjectWithoutTickets(commit);
				commitViewModel.Tags = GetTags(commit);
				commitViewModel.Tickets = GetTickets(commit);


				commitIdToRowIndex[commit.Id] = rowIndex;
			}
		}
	}
}