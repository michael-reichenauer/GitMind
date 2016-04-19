using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization.Advanced;
using GitMind.DataModel;
using GitMind.DataModel.Private;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Settings;
using GitMind.Utils;
using GitMind.Utils.UI;
using GitMind.VirtualCanvas;


namespace GitMind.CommitsHistory
{
	internal class HistoryViewModel : ViewModel, ILogViewModel
	{
		private static readonly int branchBaseIndex = 1000000;
		private static readonly int mergeBaseIndex = 2000000;

		private readonly IModelService modelService;
		private readonly IGitService gitService;
		private readonly IBrushService brushService;
		private readonly IDiffService diffService;
		private readonly ICoordinateConverter coordinateConverter;

		private double width = 1000;

		private Model model;
		private int currentCommitId = 0;
		private int currentBranchId = 0;
		private int currentMergeId = 0;
		private readonly List<CommitViewModel> commits = new List<CommitViewModel>();
		private readonly Dictionary<string, int> commitIdToRowIndex = new Dictionary<string, int>();

		private readonly Dictionary<string, int> commitIdToItemId = new Dictionary<string, int>();
		private readonly Dictionary<int, CommitViewModel> itemIdToCommit =
			new Dictionary<int, CommitViewModel>();


		private readonly List<BranchViewModel> branches = new List<BranchViewModel>();
		private readonly List<MergeViewModel> merges = new List<MergeViewModel>();
		private readonly List<string> activeBrancheNames = new List<string>();


		public HistoryViewModel()
			: this(
					new ModelService(),
					new GitService(),
					new BrushService(),
					new DiffService(),
					new CoordinateConverter())
		{
		}


		public HistoryViewModel(
			IModelService modelService,
			IGitService gitService,
			IBrushService brushService,
			IDiffService diffService,
			ICoordinateConverter coordinateConverter)
		{
			this.modelService = modelService;
			this.gitService = gitService;
			this.brushService = brushService;
			this.diffService = diffService;
			this.coordinateConverter = coordinateConverter;

			ItemsSource = new LogItemsSource(this);
		}


		public ICommand ShowBranchCommand => Command<string>(ShowBranch);

		public ICommand HideBranchCommand => Command<string>(HideBranch);

		public ObservableCollection<BranchName> AllBranches { get; }
			= new ObservableCollection<BranchName>();

		public ItemsSource ItemsSource { get; }


		// The virtual area rectangle, which would be needed to show all commits
		private Rect VirtualExtent { get; set; } = ItemsSource.EmptyExtent;

		public async Task HideBranchNameAsync(string branchName)
		{
			if (!activeBrancheNames.Contains(branchName))
			{
				return;
			}

			model = await modelService.WithRemoveBranchNameAsync(model, branchName);

			UpdateUIModel();
		}



		public async Task ToggleAsync(int column, int rowIndex, bool isControl)
		{
			// Log.Debug($"Clicked at {column},{rowIndex}");
			if (rowIndex < 0 || rowIndex >= commits.Count || column < 0 || column >= branches.Count)
			{
				// Not within supported area
				return;
			}

			CommitViewModel commitViewModel = commits[rowIndex];

			if (commitViewModel.IsMergePoint && commitViewModel.BranchColumn == column)
			{
				// User clicked on a merge point (toggle between expanded and collapsed)
				Log.Debug($"Clicked at {column},{rowIndex}, {commitViewModel}");

				if (!isControl && commitViewModel.Commit.Parents.Count == 2)
				{
					model = await modelService.WithToggleCommitAsync(model, commitViewModel.Commit);

					UpdateUIModel();
					return;
				}
			}

			if (isControl && commitViewModel.Commit.Id == commitViewModel.Commit.Branch.LatestCommit.Id
				&& activeBrancheNames.Count > 1)
			{
				// User clicked on latest commit point on a branch, which will close the branch 
				activeBrancheNames.Remove(commitViewModel.Commit.Branch.Name);

				UpdateUIModel();
			}
		}


		public void SetBranches(IReadOnlyList<string> activeBranches)
		{
			activeBrancheNames.Clear();
			activeBrancheNames.AddRange(activeBranches);
		}


		public IReadOnlyList<string> GetAllBranchNames()
		{
			return model.AllBranchNames;
		}


		public async Task LoadAsync(Window mainWindow)
		{
			while (true)
			{
				List<string> branchNames = activeBrancheNames.ToList();
				Result<IGitRepo> gitRepo = await gitService.GetRepoAsync(null, false);

				if (gitRepo.HasValue)
				{
					model = await modelService.GetModelAsync(gitRepo.Value, branchNames);
					UpdateUIModel();
					ProgramSettings.SetLatestUsedWorkingFolderPath(Environment.CurrentDirectory);
					return;
				}
				else if (gitRepo.Error == gitService.GitCommandError)
				{
					// Could not locate a local working folder
					model = Model.None;
					UpdateUIModel();

					var dialog = new System.Windows.Forms.FolderBrowserDialog();
					dialog.Description = "Please select a working folder, with an existing git repository.";
					dialog.ShowNewFolderButton = false;
					dialog.SelectedPath = Environment.GetFolderPath(
						Environment.SpecialFolder.MyDocuments);
					if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
					{
						Environment.CurrentDirectory = dialog.SelectedPath;
					}
					else
					{
						Application.Current.Shutdown(1);
						return;
					}
				}
				else if (gitRepo.Error == gitService.GitNotInstalledError)
				{
					// Could not locate a compatible installed git executable
					model = Model.None;

					UpdateUIModel();

					MessageBox.Show(
						mainWindow,
						"Could not locate a compatible git installation.",
						ProgramPaths.ProgramName,
						MessageBoxButton.OK,
						MessageBoxImage.Error);
					Application.Current.Shutdown(1);
					return;
				}
				else
				{
					Log.Warn($"Error: {gitRepo.Error}");
				}
			}
		}


		public async Task RefreshAsync(bool isShift)
		{
			Result<IGitRepo> gitRepo = await gitService.GetRepoAsync(null, isShift);
			if (gitRepo.HasValue)
			{
				Model currentModel = model;

				if (currentModel != null)
				{
					model = await modelService.RefreshAsync(gitRepo.Value, currentModel);

					UpdateUIModel();
				}
			}
		}


		private int GetBranchColumnForBranchName(string branchName)
		{
			for (int i = 0; i < model.Branches.Count; i++)
			{
				if (model.Branches[i].Name == branchName)
				{
					return i;
				}
			}

			return 0;
		}



		/// <summary>
		/// Returns range of item ids, which are visible in the area currently shown
		/// </summary>
		private IEnumerable<int> GetItemIds(Rect viewArea)
		{
			if (VirtualExtent != ItemsSource.EmptyExtent && viewArea != Rect.Empty)
			{
				// Get the part of the rectangle that is visible
				viewArea.Intersect(VirtualExtent);

				int topRowIndex = coordinateConverter.GetTopRowIndex(viewArea, commits.Count);
				int bottomRowIndex = coordinateConverter.GetBottomRowIndex(viewArea, commits.Count);

				if (bottomRowIndex > topRowIndex)
				{
					// Return visible branches
					foreach (BranchViewModel branch in branches)
					{
						if (IsVisable(topRowIndex, bottomRowIndex, branch.LatestRowIndex, branch.FirstRowIndex))
						{
							yield return branch.BranchId + branchBaseIndex;
						}
					}

					// Return visible merges
					foreach (MergeViewModel merge in merges)
					{
						if (IsVisable(topRowIndex, bottomRowIndex, merge.ChildRowIndex, merge.ParentRowIndex))
						{
							yield return merge.MergeId + mergeBaseIndex;
						}
					}


					// Return visible commits
					for (int i = topRowIndex; i <= bottomRowIndex; i++)
					{
						if (i >= 0 && i < commits.Count)
						{
							int itemId = commits[i].ItemId;
							yield return itemId;
						}
					}
				}
			}
		}

		/// <summary>
		/// Returns the item (commit, branch, merge) corresponding to the specified id.
		/// Commits are are in the 0->branchBaseIndex-1 range
		/// Branches are in the branchBaseIndex->mergeBaseIndex-1 range
		/// Merges are mergeBaseIndex-> ... range
		/// </summary>
		private object GetItem(int id)
		{
			if (commits.Count == 0)
			{
				// No items yet
				return null;
			}

			if (id < branchBaseIndex)
			{
				// An item in the commit row range
				CommitViewModel commit;
				itemIdToCommit.TryGetValue(id, out commit);
				return commit;
			}
			else if (id < mergeBaseIndex)
			{
				// An item in the branch range
				int branchId = id - branchBaseIndex;

				return branches.FirstOrDefault(b => b.BranchId == branchId);
			}
			else
			{
				// An item in the merge range
				int mergeId = id - mergeBaseIndex;
			
				return merges.FirstOrDefault(m => m.MergeId == mergeId);
			}
		}



		private async void ShowBranch(string branchName)
		{
			if (activeBrancheNames.Contains(branchName))
			{
				return;
			}

			model = await modelService.WithAddBranchNameAsync(model, branchName, null);

			UpdateUIModel();
		}


		private async void HideBranch(string branchName)
		{
			await HideBranchNameAsync(branchName);
		}


		private static bool IsVisable(int topRow, int bottomRow, int topLineIndex, int bottomLineIndex)
		{
			return
				(topLineIndex >= topRow && topLineIndex <= bottomRow)
				|| (bottomLineIndex >= topRow && bottomLineIndex <= bottomRow)
				|| (topLineIndex <= topRow && bottomLineIndex >= bottomRow);
		}



		private void UpdateUIModel()
		{
			activeBrancheNames.Clear();

			foreach (BranchBuilder branch in model.Branches)
			{
				activeBrancheNames.Add(branch.Name);
			}

			commits.Clear();
			commitIdToRowIndex.Clear();

			branches.Clear();
			merges.Clear();

			CreateRows();
			CreateBranches();
			CreateMerges();

			AllBranches.Clear();
			List<string> allBranchNames = GetAllBranchNames().ToList();
			allBranchNames.Sort();
			foreach (string branchName in allBranchNames)
			{
				AllBranches.Add(new BranchName(branchName));
			}

			DataChanged();
		}


		private void DataChanged()
		{
			VirtualExtent = new Rect(0, 0, Width, coordinateConverter.GetRowExtent(commits.Count));

			ItemsSource.TriggerInvalidated();
		}


		private void CreateRows()
		{
			for (int rowIndex = 0; rowIndex < model.Commits.Count; rowIndex++)
			{
				Commit commit = model.Commits[rowIndex];

				CommitViewModel commitViewModel = GetCommitViewModel(commit);

				commitViewModel.Commit = commit;
				commitViewModel.Rect = new Rect(
					0,
					coordinateConverter.ConvertFromRow(rowIndex),
					Width - 35,
					coordinateConverter.ConvertFromRow(1));

				commitViewModel.IsMergePoint = commit.Parents.Count > 1
					&& (!commit.SecondParent.IsOnActiveBranch()
						|| commit.Branch != commit.SecondParent.Branch);
				commitViewModel.IsCurrent = commit == model.CurrentCommit;

				commitViewModel.BranchColumn = GetBranchColumnForBranchName(commit.Branch.Name);
				commitViewModel.GraphWidth = coordinateConverter.ConvertFromColumn(model.Branches.Count);

				commitViewModel.Size = commitViewModel.IsMergePoint ? 10 : 6;
				commitViewModel.XPoint = commitViewModel.IsMergePoint ?
					2 + coordinateConverter.ConvertFromColumn(commitViewModel.BranchColumn) :
					4 + coordinateConverter.ConvertFromColumn(commitViewModel.BranchColumn);
				commitViewModel.YPoint = commitViewModel.IsMergePoint ? 2 : 4;

				commitViewModel.Brush = brushService.GetBRanchBrush(commit.Branch);
				commitViewModel.BrushInner = commit.IsExpanded
					? brushService.GetDarkerBrush(commitViewModel.Brush) : commitViewModel.Brush;

				commitViewModel.SubjectBrush = GetSubjectBrush(commit);
				commitViewModel.Width = Width - 35;
				commitViewModel.ToolTip = GetCommitToolTip(commit);

				commitViewModel.Date = GetCommitDate(commit);
				commitViewModel.Subject = GetSubjectWithoutTickets(commit);
				commitViewModel.Tags = GetTags(commit);
				commitViewModel.Tickets = GetTickets(commit);
				commitViewModel.CommitBranchText = "Hide branch: " + commit.Branch.Name;
				commitViewModel.CommitBranchName = commit.Branch.Name;


				commits.Add(commitViewModel);
				commitIdToRowIndex[commit.Id] = rowIndex;
			}
		}


		private static string GetCommitDate(Commit commit)
		{
			return commit.DateTime.ToShortDateString()
						 + " " + commit.DateTime.ToShortTimeString();
		}


		private string GetSubjectWithoutTickets(Commit commit)
		{
			string tickets = GetTickets(commit);
			return commit.Subject.Substring(tickets.Length);
		}


		private static string GetTags(Commit commit)
		{
			return commit.Tags.Count == 0
				? ""
				: "[" + string.Join("],[", commit.Tags.Select(t => t.Text)) + "] ";
		}


		private string GetTickets(Commit commit)
		{
			if (commit.Subject.StartsWith("#"))
			{
				int index = commit.Subject.IndexOf(" ");
				if (index > 1)
				{
					return commit.Subject.Substring(0, index);
				}
				if (index > 0)
				{
					index = commit.Subject.IndexOf(" ", index + 1);
					return commit.Subject.Substring(0, index);
				}
			}

			return "";
		}


		private CommitViewModel GetCommitViewModel(Commit commit)
		{
			CommitViewModel commitViewModel;
			int itemId = 0;
			if (!commitIdToItemId.TryGetValue(commit.Id, out itemId))
			{
				itemId = ++currentCommitId;
				commitIdToItemId[commit.Id] = itemId;

				commitViewModel = new CommitViewModel(
					itemId,
					HideBranchNameAsync,
					ShowDiffAsync);

				itemIdToCommit[itemId] = commitViewModel;
			}
			else
			{
				commitViewModel = itemIdToCommit[itemId];
			}
			return commitViewModel;
		}


		private async Task ShowDiffAsync(string commitId)
		{
			await diffService.ShowDiffAsync(commitId);
		}


		public Brush GetSubjectBrush(Commit commit)
		{
			Brush subjectBrush = brushService.SubjectBrush;
			if (commit.IsLocalAhead)
			{
				subjectBrush = brushService.LocalAheadBrush;
			}
			else if (commit.IsRemoteAhead)
			{
				subjectBrush = brushService.RemoteAheadBrush;
			}

			return subjectBrush;
		}

		private static string GetCommitToolTip(Commit commit)
		{
			string name = commit.Branch.IsMultiBranch ? "MultiBranch" : commit.Branch.Name;
			string toolTip = $"Commit id: {commit.ShortId}\nBranch: {name}";
			if (commit.Branch.LocalAheadCount > 0)
			{
				toolTip += $"\nAhead: {commit.Branch.LocalAheadCount}";
			}
			if (commit.Branch.RemoteAheadCount > 0)
			{
				toolTip += $"\nBehind: {commit.Branch.RemoteAheadCount}";
			}
			return toolTip;
		}


		private void CreateBranches()
		{
			for (int i = 0; i < model.Branches.Count; i++)
			{
				IBranch branch = model.Branches[i];
				int branchId = ++currentBranchId;
				int latestRowIndex = commitIdToRowIndex[branch.LatestCommit.Id];
				int firstRowIndex = commitIdToRowIndex[branch.FirstCommit.Id];
				int height = coordinateConverter.ConvertFromRow(firstRowIndex - latestRowIndex);

				BranchViewModel branchViewModel = new BranchViewModel(
					branch.Name,
					branchId,
					i,
					latestRowIndex,
					firstRowIndex,
					new Rect(
						(double)coordinateConverter.ConvertFromColumn(i) + 5,
						(double)coordinateConverter.ConvertFromRow(latestRowIndex) + CoordinateConverter.HalfRow,
						6,
						height),
					line: $"M 2,0 L 2,{height}",
					brush: brushService.GetBRanchBrush(branch),
					branchToolTip: GetBranchToolTip(branch));

				branches.Add(branchViewModel);
			}
		}


		private void CreateMerges()
		{
			for (int i = 0; i < model.Merges.Count; i++)
			{
				Merge merge = model.Merges[i];
				int mergeId = ++currentMergeId;

				int parentRowIndex = commitIdToRowIndex[merge.ParentCommit.Id];
				BranchBuilder parentBranch = merge.ParentCommit.Branch;
				int parrentColumn = branches.First(b => b.Name == parentBranch.Name).BranchColumn;

				int childRowIndex = commitIdToRowIndex[merge.ChildCommit.Id];
				BranchBuilder childBranch = merge.ChildCommit.Branch;
				int childColumn = branches.First(b => b.Name == childBranch.Name).BranchColumn;

				BranchBuilder mainBranch = childColumn > parrentColumn ? childBranch : parentBranch;

				int xx1 = coordinateConverter.ConvertFromColumn(childColumn);
				int xx2 = coordinateConverter.ConvertFromColumn(parrentColumn);

				int x1 = xx1 < xx2 ? 0 : xx1 - xx2 - 6;
				int x2 = xx2 < xx1 ? 0 : xx2 - xx1 - 6;
				int y1 = 0;
				int y2 = coordinateConverter.ConvertFromRow(parentRowIndex - childRowIndex) + CoordinateConverter.HalfRow - 8;

				if (merge.IsMain)
				{
					y1 = y1 + 2;
					x1 = x1 + 2;
				}

				MergeViewModel mergeViewModel = new MergeViewModel(
					mergeId,
					parentRowIndex,
					childRowIndex,
					new Rect(
						(double)Math.Min(xx1, xx2) + 10,
						(double)coordinateConverter.ConvertFromRow(childRowIndex) + CoordinateConverter.HalfRow,
						 Math.Abs(xx1 - xx2) + 2,
						y2 + 2),
					line: $"M {x1},{y1} L {x2},{y2}",
					brush: brushService.GetBRanchBrush(mainBranch),
					stroke: merge.IsMain ? 2 : 1,
					strokeDash: merge.IsVirtual ? "4,2" : "");

				merges.Add(mergeViewModel);
			}
		}


		private static string GetBranchToolTip(IBranch branch)
		{
			string name = branch.IsMultiBranch ? "MultiBranch" : branch.Name;
			string toolTip = $"Branch: {name}";
			if (branch.LocalAheadCount > 0)
			{
				toolTip += $"\nAhead: {branch.LocalAheadCount}";
			}
			if (branch.RemoteAheadCount > 0)
			{
				toolTip += $"\nBehind: {branch.RemoteAheadCount}";
			}
			return toolTip;
		}


		public double Width
		{
			get { return width; }
			set
			{
				width = value;

				if (model != null)
				{
					UpdateUIModel();
				}
			}
		}


		public async Task ClickedAsync(Point position, bool isControl)
		{
			double xpos = position.X - 9;
			double ypos = position.Y - 5;

			int column = coordinateConverter.ConvertToColumn(xpos);
			int x = coordinateConverter.ConvertFromColumn(column);

			int row = coordinateConverter.ConvertToRow(ypos);
			int y = coordinateConverter.ConvertFromRow(row) + 10;

			double absx = Math.Abs(xpos - x);
			double absy = Math.Abs(ypos - y);

			if ((absx < 10) && (absy < 10))
			{
				await ToggleAsync(column, row, isControl);
			}
		}



		private class LogItemsSource : ItemsSource
		{
			private readonly HistoryViewModel instance;

			public LogItemsSource(HistoryViewModel instance)
			{
				this.instance = instance;
			}

			protected override Rect VirtualExtent => instance.VirtualExtent;

			protected override IEnumerable<int> GetItemIds(Rect viewArea)
				=> instance.GetItemIds(viewArea);

			protected override object GetItem(int id) => instance.GetItem(id);
		}
	}
}

