//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Threading;
//using GitMind.DataModel.Old;
//using GitMind.Git;
//using GitMind.Git.Private;
//using GitMind.Settings;
//using GitMind.Utils;
//using GitMind.Utils.UI;
//using GitMind.VirtualCanvas;


//namespace GitMind.CommitsHistory
//{
//	internal class OldHistoryViewModel : ViewModel, IOldHistoryViewModel
//	{
//		private static readonly int branchBaseIndex = 1000000;
//		private static readonly int mergeBaseIndex = 2000000;

//		private readonly IOldModelService modelService;
//		private readonly IGitService gitService;
//		private readonly IBrushService brushService;
//		private readonly IDiffService diffService;

//		private double width = 1000;

//		private OldModel model;
//		private bool isUpdateing;
//		private int currentBranchId = 0;
//		private int currentMergeId = 0;
//		private readonly List<OldCommitViewModel> commits = new List<OldCommitViewModel>();
//		private readonly Dictionary<string, int> commitIdToRowIndex = new Dictionary<string, int>();

//		private readonly List<OldBranchViewModel> branches = new List<OldBranchViewModel>();
//		private readonly List<OldMergeViewModel> merges = new List<OldMergeViewModel>();
//		private readonly List<string> activeBrancheNames = new List<string>();
//		private readonly DispatcherTimer filterTriggerTimer = new DispatcherTimer();
//		private string filterText = "";

//		public OldHistoryViewModel()
//			: this(
//					new OldModelService(),
//					new GitService(),
//					new BrushService(),
//					new DiffService())
//		{
//		}


//		public OldHistoryViewModel(
//			IOldModelService modelService,
//			IGitService gitService,
//			IBrushService brushService,
//			IDiffService diffService)
//		{
//			this.modelService = modelService;
//			this.gitService = gitService;
//			this.brushService = brushService;
//			this.diffService = diffService;
//			filterTriggerTimer.Tick += FilterTrigger;

//			ItemsSource = new LogItemsSource(this);
//		}



//		public ICommand ShowBranchCommand => Command<string>(ShowBranch);

//		public ICommand HideBranchCommand => Command<string>(HideBranch);

//		public ICommand ToggleDetailsCommand => Command(ToggleDetails);


//		private void ToggleDetails()
//		{
//			DetailsSize = DetailsSize > 0 ? 0 : 150;
//		}


//		public int DetailsSize
//		{
//			get { return Get(); }
//			set { Set(value); }
//		}


//		public ObservableCollection<BranchName> AllBranches { get; }
//			= new ObservableCollection<BranchName>();

//		public VirtualItemsSource ItemsSource { get; }

//		public int SelectedIndex
//		{
//			get { return Get(); }
//			set
//			{
//				Log.Debug($"Setting value {value}");
//				OldCommitViewModel commit = commits[value];

//				CommitDetail.Id = commit.Id;
//				CommitDetail.Branch = commit.Commit.Branch.Name;
//				CommitDetail.Tickets = commit.Tickets;
//				CommitDetail.Tags = commit.Tags;
//				CommitDetail.Subject = commit.Subject;
//			}
//		}


//		public CommitDetailViewModel CommitDetail { get; } = new CommitDetailViewModel(null);

//		// The virtual area rectangle, which would be needed to show all commits
//		private Rect VirtualExtent { get; set; } = VirtualItemsSource.EmptyExtent;

//		public async Task HideBranchNameAsync(string branchName)
//		{
//			if (!activeBrancheNames.Contains(branchName))
//			{
//				return;
//			}

//			model = await modelService.WithRemoveBranchNameAsync(model, branchName);

//			UpdateUIModel();
//		}


//		public void SetFilter(string text)
//		{
//			filterTriggerTimer.Stop();
//			filterText = (text ?? "").Trim();
//			filterTriggerTimer.Interval = TimeSpan.FromMilliseconds(500);
//			filterTriggerTimer.Start();
//		}


//		private void FilterTrigger(object sender, EventArgs e)
//		{
//			filterTriggerTimer.Stop();

//			UpdateUIModel();
//		}


//		public async Task ToggleAsync(int column, int rowIndex, bool isControl)
//		{
//			// Log.Debug($"Clicked at {column},{rowIndex}");
//			if (rowIndex < 0 || rowIndex >= commits.Count || column < 0 || column >= branches.Count)
//			{
//				// Not within supported area
//				return;
//			}

//			OldCommitViewModel commitViewModel = commits[rowIndex];

//			if (commitViewModel.IsMergePoint && commitViewModel.BranchColumn == column)
//			{
//				// User clicked on a merge point (toggle between expanded and collapsed)
//				Log.Debug($"Clicked at {column},{rowIndex}, {commitViewModel}");

//				if (!isControl && commitViewModel.Commit.Parents.Count == 2)
//				{
//					model = await modelService.WithToggleCommitAsync(model, commitViewModel.Commit);

//					UpdateUIModel();
//					return;
//				}
//			}

//			if (isControl && commitViewModel.Commit.Id == commitViewModel.Commit.Branch.LatestCommit.Id
//				&& activeBrancheNames.Count > 1)
//			{
//				// User clicked on latest commit point on a branch, which will close the branch 
//				activeBrancheNames.Remove(commitViewModel.Commit.Branch.Name);

//				UpdateUIModel();
//			}
//		}


//		public void SetBranches(IReadOnlyList<string> activeBranches)
//		{
//			activeBrancheNames.Clear();
//			activeBrancheNames.AddRange(activeBranches);
//		}


//		public IReadOnlyList<string> GetAllBranchNames()
//		{
//			return model.AllBranchNames;
//		}


//		public async Task LoadAsync(Window mainWindow)
//		{
//			while (true)
//			{
//				R<string> currentRootPath = gitService.GetCurrentRootPath(null);

//				if (currentRootPath.HasValue)
//				{
//					List<string> branchNames = activeBrancheNames.ToList();

//					model = await modelService.GetCachedModelAsync(branchNames);

//					UpdateUIModel();
//					SelectedIndex = 0;
//					ProgramSettings.SetLatestUsedWorkingFolderPath(Environment.CurrentDirectory);
//					return;
//				}
//				else if (currentRootPath.Error == gitService.GitCommandError)
//				{
//					// Could not locate a local working folder
//					model = OldModel.None;
//					UpdateUIModel();

//					var dialog = new System.Windows.Forms.FolderBrowserDialog();
//					dialog.Description = "Please select a working folder, with an existing git repository.";
//					dialog.ShowNewFolderButton = false;
//					dialog.SelectedPath = Environment.GetFolderPath(
//						Environment.SpecialFolder.MyDocuments);
//					if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
//					{
//						Environment.CurrentDirectory = dialog.SelectedPath;
//					}
//					else
//					{
//						Application.Current.Shutdown(1);
//						return;
//					}
//				}
//				else if (currentRootPath.Error == gitService.GitNotInstalledError)
//				{
//					// Could not locate a compatible installed git executable
//					model = OldModel.None;

//					UpdateUIModel();

//					MessageBox.Show(
//						mainWindow,
//						"Could not locate a compatible git installation.",
//						ProgramPaths.ProgramName,
//						MessageBoxButton.OK,
//						MessageBoxImage.Error);
//					Application.Current.Shutdown(1);
//					return;
//				}
//				else
//				{
//					Log.Warn($"Error: {currentRootPath.Error}");
//				}
//			}
//		}


//		public async Task RefreshAsync(bool isShift)
//		{
//			if (isUpdateing)
//			{
//				return;
//			}

//			try
//			{
//				isUpdateing = true;
//			}
//			finally
//			{
//				if (isShift)
//				{
//					await gitService.FetchAsync(null);
//				}

//				OldModel currentModel = model;

//				if (currentModel != null)
//				{
//					model = await modelService.RefreshAsync(currentModel);

//					UpdateUIModel();
//				}
//				isUpdateing = false;
//			}
//		}


//		private int GetBranchColumnForBranchName(string branchName)
//		{
//			for (int i = 0; i < model.Branches.Count; i++)
//			{
//				if (model.Branches[i].Name == branchName)
//				{
//					return i;
//				}
//			}

//			return 0;
//		}



//		/// <summary>
//		/// Returns range of item ids, which are visible in the area currently shown
//		/// </summary>
//		private IEnumerable<int> GetItemIds(Rect viewArea)
//		{
//			if (VirtualExtent != VirtualItemsSource.EmptyExtent && viewArea != Rect.Empty)
//			{
//				// Get the part of the rectangle that is visible
//				viewArea.Intersect(VirtualExtent);

//				int topRowIndex = Converter.ToTopRowIndex(viewArea, commits.Count);
//				int bottomRowIndex = Converter.ToBottomRowIndex(viewArea, commits.Count);

//				if (bottomRowIndex > topRowIndex)
//				{
//					// Return visible branches
//					foreach (OldBranchViewModel branch in branches)
//					{
//						if (IsVisable(topRowIndex, bottomRowIndex, branch.LatestRowIndex, branch.FirstRowIndex))
//						{
//							yield return branch.BranchId + branchBaseIndex;
//						}
//					}

//					// Return visible merges
//					foreach (OldMergeViewModel merge in merges)
//					{
//						if (IsVisable(topRowIndex, bottomRowIndex, merge.ChildRowIndex, merge.ParentRowIndex))
//						{
//							yield return merge.MergeId + mergeBaseIndex;
//						}
//					}


//					// Return visible commits
//					for (int i = topRowIndex; i <= bottomRowIndex; i++)
//					{
//						if (i >= 0 && i < commits.Count)
//						{
//							yield return i;
//						}
//					}
//				}
//			}
//		}

//		/// <summary>
//		/// Returns the item (commit, branch, merge) corresponding to the specified id.
//		/// Commits are are in the 0->branchBaseIndex-1 range
//		/// Branches are in the branchBaseIndex->mergeBaseIndex-1 range
//		/// Merges are mergeBaseIndex-> ... range
//		/// </summary>
//		private object GetItem(int id)
//		{
//			if (commits.Count == 0)
//			{
//				// No items yet
//				return null;
//			}

//			if (id < branchBaseIndex)
//			{
//				if (commits.Count > 0 && id >= 0 && id < commits.Count)
//				{
//					return commits[id];
//				}
//			}
//			else if (id < mergeBaseIndex)
//			{
//				// An item in the branch range
//				int branchId = id - branchBaseIndex;

//				return branches.FirstOrDefault(b => b.BranchId == branchId);
//			}
//			else
//			{
//				// An item in the merge range
//				int mergeId = id - mergeBaseIndex;

//				return merges.FirstOrDefault(m => m.MergeId == mergeId);
//			}

//			return null;
//		}



//		private async void ShowBranch(string branchName)
//		{
//			if (activeBrancheNames.Contains(branchName))
//			{
//				return;
//			}

//			model = await modelService.WithAddBranchNameAsync(model, branchName, null);

//			UpdateUIModel();
//		}


//		private async void HideBranch(string branchName)
//		{
//			await HideBranchNameAsync(branchName);
//		}


//		private static bool IsVisable(int topRow, int bottomRow, int topLineIndex, int bottomLineIndex)
//		{
//			return
//				(topLineIndex >= topRow && topLineIndex <= bottomRow)
//				|| (bottomLineIndex >= topRow && bottomLineIndex <= bottomRow)
//				|| (topLineIndex <= topRow && bottomLineIndex >= bottomRow);
//		}



//		private void UpdateUIModel()
//		{
//			activeBrancheNames.Clear();

//			foreach (OldBranchBuilder branch in model.Branches)
//			{
//				activeBrancheNames.Add(branch.Name);
//			}

//			commitIdToRowIndex.Clear();

//			branches.Clear();
//			merges.Clear();

//			CreateRows();

//			if (string.IsNullOrWhiteSpace(filterText))
//			{
//				CreateBranches();
//				CreateMerges();
//			}

//			AllBranches.Clear();
//			List<string> allBranchNames = GetAllBranchNames().ToList();
//			allBranchNames.Sort();
//			foreach (string branchName in allBranchNames)
//			{
//				AllBranches.Add(new BranchName(branchName));
//			}

//			DataChanged();
//		}


//		private void DataChanged()
//		{
//			VirtualExtent = new Rect(0, 0, Width, Converter.ToRowExtent(commits.Count));

//			ItemsSource.TriggerInvalidated();
//		}


//		private void CreateRows()
//		{
//			int graphWidth = Converter.ToX(model.Branches.Count);

//			IReadOnlyList<OldCommit> sourceCommits = model.Commits;

//			if (!string.IsNullOrWhiteSpace(filterText))
//			{
//				sourceCommits = model.GitRepo.GetAllCommts()
//					.Where(c => c.Subject.IndexOf(filterText, StringComparison.CurrentCultureIgnoreCase) != -1
//					|| c.Author.IndexOf(filterText, StringComparison.CurrentCultureIgnoreCase) != -1
//					|| c.Id.StartsWith(filterText, StringComparison.CurrentCultureIgnoreCase))
//					.Select(c => model.GetCommit(c.Id))
//					.ToList();
//			}


//			int commitsCount = sourceCommits.Count;
//			SetNumberOfCommit(commitsCount);

//			for (int rowIndex = 0; rowIndex < commitsCount; rowIndex++)
//			{
//				OldCommit commit = sourceCommits[rowIndex];

//				OldCommitViewModel commitViewModel = commits[rowIndex];

//				commitViewModel.Commit = commit;
//				commitViewModel.Id = commit.Id;
//				commitViewModel.Rect = new Rect(
//					0,
//					Converter.ToY(rowIndex),
//					Width - 35,
//					Converter.ToY(1));

//				commitViewModel.IsCurrent = commit == model.CurrentCommit;

//				if (string.IsNullOrWhiteSpace(filterText))
//				{
//					commitViewModel.IsMergePoint = commit.Parents.Count > 1
//						&& (!commit.SecondParent.IsOnActiveBranch()
//						|| commit.Branch != commit.SecondParent.Branch);

//					commitViewModel.BranchColumn = GetBranchColumnForBranchName(commit.Branch.Name);

//					commitViewModel.Size = commitViewModel.IsMergePoint ? 10 : 6;
//					commitViewModel.XPoint = commitViewModel.IsMergePoint
//						? 2 + Converter.ToX(commitViewModel.BranchColumn)
//						: 4 + Converter.ToX(commitViewModel.BranchColumn);
//					commitViewModel.YPoint = commitViewModel.IsMergePoint ? 2 : 4;

//					commitViewModel.Brush = brushService.GetBranchBrush(commit.Branch);
//					commitViewModel.BrushInner = commit.IsExpanded
//						? brushService.GetDarkerBrush(commitViewModel.Brush)
//						: commitViewModel.Brush;

//					commitViewModel.CommitBranchText = "Hide branch: " + commit.Branch.Name;
//					commitViewModel.CommitBranchName = commit.Branch.Name;
//					commitViewModel.ToolTip = GetCommitToolTip(commit);
//					commitViewModel.SubjectBrush = GetSubjectBrush(commit);
//				}
//				else
//				{
//					commitViewModel.SubjectBrush = brushService.SubjectBrush;
//					commitViewModel.IsMergePoint = false;
//					commitViewModel.BranchColumn = 0;
//					commitViewModel.Size = 0;
//					commitViewModel.XPoint = 0;
//					commitViewModel.YPoint = 0;
//					commitViewModel.Brush = Brushes.Black;
//					commitViewModel.BrushInner = Brushes.Black;
//					commitViewModel.CommitBranchText = "";
//					commitViewModel.CommitBranchName = "";
//					commitViewModel.ToolTip = "";
//				}

//				commitViewModel.GraphWidth = graphWidth;


//				commitViewModel.Width = Width - 35;

//				commitViewModel.Date = GetCommitDate(commit);
//				commitViewModel.Author = commit.Author;
//				commitViewModel.Subject = GetSubjectWithoutTickets(commit);
//				commitViewModel.Tags = GetTags(commit);
//				commitViewModel.Tickets = GetTickets(commit);


//				commitIdToRowIndex[commit.Id] = rowIndex;
//			}
//		}


//		private void SetNumberOfCommit(int capacity)
//		{
//			if (commits.Count > capacity)
//			{
//				// To many items, lets remove the rows no longer needed
//				commits.RemoveRange(capacity, commits.Count - capacity);
//				return;
//			}

//			if (commits.Count < capacity)
//			{
//				commits.Capacity = capacity;
//				// To few items, lets create the rows needed
//				int lowIndex = commits.Count;
//				for (int i = lowIndex; i < capacity; i++)
//				{
//					commits.Add(new OldCommitViewModel(HideBranchNameAsync, ShowDiffAsync));
//				}
//			}
//		}


//		private static string GetCommitDate(OldCommit commit)
//		{
//			return commit.DateTime.ToShortDateString()
//						 + " " + commit.DateTime.ToShortTimeString();
//		}


//		private string GetSubjectWithoutTickets(OldCommit commit)
//		{
//			string tickets = GetTickets(commit);
//			return commit.Subject.Substring(tickets.Length);
//		}


//		private static string GetTags(OldCommit commit)
//		{
//			return commit.Tags.Count == 0
//				? ""
//				: "[" + string.Join("],[", commit.Tags.Select(t => t.Text)) + "] ";
//		}


//		private string GetTickets(OldCommit commit)
//		{
//			if (commit.Subject.StartsWith("#"))
//			{
//				int index = commit.Subject.IndexOf(" ");
//				if (index > 1)
//				{
//					return commit.Subject.Substring(0, index);
//				}
//				if (index > 0)
//				{
//					index = commit.Subject.IndexOf(" ", index + 1);
//					return commit.Subject.Substring(0, index);
//				}
//			}

//			return "";
//		}


//		private async Task ShowDiffAsync(string commitId)
//		{
//			await diffService.ShowDiffAsync(commitId);
//		}


//		public Brush GetSubjectBrush(OldCommit commit)
//		{
//			Brush subjectBrush = brushService.SubjectBrush;
//			if (commit.IsLocalAhead)
//			{
//				subjectBrush = brushService.LocalAheadBrush;
//			}
//			else if (commit.IsRemoteAhead)
//			{
//				subjectBrush = brushService.RemoteAheadBrush;
//			}

//			return subjectBrush;
//		}

//		private static string GetCommitToolTip(OldCommit commit)
//		{
//			string name = commit.Branch.IsMultiBranch ? "MultiBranch" : commit.Branch.Name;
//			string toolTip = $"Commit id: {commit.ShortId}\nBranch: {name}";
//			if (commit.Branch.LocalAheadCount > 0)
//			{
//				toolTip += $"\nAhead: {commit.Branch.LocalAheadCount}";
//			}
//			if (commit.Branch.RemoteAheadCount > 0)
//			{
//				toolTip += $"\nBehind: {commit.Branch.RemoteAheadCount}";
//			}
//			return toolTip;
//		}


//		private void CreateBranches()
//		{
//			for (int i = 0; i < model.Branches.Count; i++)
//			{
//				IBranch branch = model.Branches[i];
//				int branchId = ++currentBranchId;
//				int latestRowIndex = commitIdToRowIndex[branch.LatestCommit.Id];
//				int firstRowIndex = commitIdToRowIndex[branch.FirstCommit.Id];
//				int height = Converter.ToY(firstRowIndex - latestRowIndex);

//				OldBranchViewModel branchViewModel = new OldBranchViewModel(
//					branch.Name,
//					branchId,
//					i,
//					latestRowIndex,
//					firstRowIndex,
//					new Rect(
//						(double)Converter.ToX(i) + 5,
//						(double)Converter.ToY(latestRowIndex) + Converter.HalfRow,
//						6,
//						height),
//					line: $"M 2,0 L 2,{height}",
//					brush: brushService.GetBranchBrush(branch),
//					branchToolTip: GetBranchToolTip(branch));

//				branches.Add(branchViewModel);
//			}
//		}


//		private void CreateMerges()
//		{
//			for (int i = 0; i < model.Merges.Count; i++)
//			{
//				OldMerge merge = model.Merges[i];
//				int mergeId = ++currentMergeId;

//				int parentRowIndex = commitIdToRowIndex[merge.ParentCommit.Id];
//				OldBranchBuilder parentBranch = merge.ParentCommit.Branch;
//				int parrentColumn = branches.First(b => b.Name == parentBranch.Name).BranchColumn;

//				int childRowIndex = commitIdToRowIndex[merge.ChildCommit.Id];
//				OldBranchBuilder childBranch = merge.ChildCommit.Branch;
//				int childColumn = branches.First(b => b.Name == childBranch.Name).BranchColumn;

//				OldBranchBuilder mainBranch = childColumn > parrentColumn ? childBranch : parentBranch;

//				int xx1 = Converter.ToX(childColumn);
//				int xx2 = Converter.ToX(parrentColumn);

//				int x1 = xx1 < xx2 ? 0 : xx1 - xx2 - 6;
//				int x2 = xx2 < xx1 ? 0 : xx2 - xx1 - 6;
//				int y1 = 0;
//				int y2 = Converter.ToY(parentRowIndex - childRowIndex) + Converter.HalfRow - 8;

//				if (merge.IsMain)
//				{
//					y1 = y1 + 2;
//					x1 = x1 + 2;
//				}

//				OldMergeViewModel mergeViewModel = new OldMergeViewModel(
//					mergeId,
//					parentRowIndex,
//					childRowIndex,
//					new Rect(
//						(double)Math.Min(xx1, xx2) + 10,
//						(double)Converter.ToY(childRowIndex) + Converter.HalfRow,
//						 Math.Abs(xx1 - xx2) + 2,
//						y2 + 2),
//					line: $"M {x1},{y1} L {x2},{y2}",
//					brush: brushService.GetBranchBrush(mainBranch),
//					stroke: merge.IsMain ? 2 : 1,
//					strokeDash: merge.IsVirtual ? "4,2" : "");

//				merges.Add(mergeViewModel);
//			}
//		}


//		private static string GetBranchToolTip(IBranch branch)
//		{
//			string name = branch.IsMultiBranch ? "MultiBranch" : branch.Name;
//			string toolTip = $"Branch: {name}";
//			if (branch.LocalAheadCount > 0)
//			{
//				toolTip += $"\nAhead: {branch.LocalAheadCount}";
//			}
//			if (branch.RemoteAheadCount > 0)
//			{
//				toolTip += $"\nBehind: {branch.RemoteAheadCount}";
//			}
//			return toolTip;
//		}


//		public double Width
//		{
//			get { return width; }
//			set
//			{
//				width = value;

//				if (model != null)
//				{
//					UpdateUIModel();
//				}
//			}
//		}


//		public async Task ClickedAsync(Point position, bool isControl)
//		{
//			double xpos = position.X - 9;
//			double ypos = position.Y - 5;

//			int column = Converter.ToColumn(xpos);
//			int x = Converter.ToX(column);

//			int row = Converter.ToRow(ypos);
//			int y = Converter.ToY(row) + 10;

//			double absx = Math.Abs(xpos - x);
//			double absy = Math.Abs(ypos - y);

//			if ((absx < 10) && (absy < 10))
//			{
//				await ToggleAsync(column, row, isControl);
//			}
//		}



//		private class LogItemsSource : VirtualItemsSource
//		{
//			private readonly OldHistoryViewModel instance;

//			public LogItemsSource(OldHistoryViewModel instance)
//			{
//				this.instance = instance;
//			}

//			protected override Rect VirtualArea => instance.VirtualExtent;

//			protected override IEnumerable<int> GetItemIds(Rect viewArea)
//				=> instance.GetItemIds(viewArea);

//			protected override object GetItem(int id) => instance.GetItem(id);
//		}
//	}
//}

