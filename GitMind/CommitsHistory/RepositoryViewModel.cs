using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class RepositoryViewModel : ViewModel
	{
		
		private readonly IViewModelService viewModelService;
	

		private readonly DispatcherTimer filterTriggerTimer = new DispatcherTimer();
		private string settingFilterText = "";
		private string filterText = "";

		private int width = 0;
		private int graphWidth = 0;

		public List<BranchViewModel> Branches { get; } = new List<BranchViewModel>();
		public List<MergeViewModel> Merges { get; } = new List<MergeViewModel>();
		public List<CommitViewModel> Commits { get; } = new List<CommitViewModel>();
		

		public Dictionary<string, CommitViewModel> CommitsById { get; } =
			new Dictionary<string, CommitViewModel>();


		public RepositoryViewModel(Action<int> setFirstVisibleRow)
			: this(new ViewModelService(), setFirstVisibleRow)
		{
		}

		public RepositoryViewModel(
			IViewModelService viewModelService,
			Action<int> scrollRows)
		{
			ScrollRows = scrollRows;
			this.viewModelService = viewModelService;
	
			VirtualItemsSource = new RepositoryVirtualItemsSource(Branches, Merges, Commits);

			filterTriggerTimer.Tick += FilterTrigger;
			filterTriggerTimer.Interval = TimeSpan.FromMilliseconds(300);
		}

		public Action<int> ScrollRows { get; }
		public Repository Repository { get; private set; } 

		public ICommand ShowBranchCommand => Command<Branch>(ShowBranch);
		public ICommand HideBranchCommand => Command<string>(HideBranch);
		public ICommand ToggleDetailsCommand => Command(ToggleDetails);


		public RepositoryVirtualItemsSource VirtualItemsSource { get; }

		public ObservableCollection<BranchName> AllBranches { get; }
			= new ObservableCollection<BranchName>();


		public CommitDetailViewModel CommitDetail { get; } = new CommitDetailViewModel(null);

		public int DetailsSize
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int Width
		{
			get { return width; }
			set
			{
				if (width != value)
				{
					width = value;
					Commits.ForEach(commit => commit.WindowWidth = width);
					VirtualItemsSource.DataChanged(width);
				}
			}
		}

		public int GraphWidth
		{
			get { return graphWidth; }
			set
			{
				if (graphWidth != value)
				{
					graphWidth = value;
					Commits.ForEach(commit => commit.GraphWidth = graphWidth);
				}

			}
		}

		public void Update(Repository repository)
		{
			Repository = repository;
			viewModelService.Update(this);
			Commits.ForEach(commit => commit.WindowWidth = Width);

			VirtualItemsSource.DataChanged(width);

			if (Commits.Any())
			{
				// ### Does not yet work but deselects the first branch at least
				SelectedItem = Commits[0];
			}
		}


		public object SelectedIndex
		{
			get { return Get(); }
			set
			{
				if (Set(value).IsSet)
				{
					Log.Debug($"Setting value index: {value}");
				}
			}
		}


		public object SelectedItem
		{
			get { return Get().Value; }
			set
			{
				if (Set(value).IsSet)
				{
					Log.Debug($"Setting value item: {value}");
				}
			}
		}

		public IReadOnlyList<Branch> SpecifiedBranches { get; set; }


		public void SetFilter(string text)
		{
			filterTriggerTimer.Stop();
			settingFilterText = (text ?? "").Trim();
			filterTriggerTimer.Start();
		}


		private void FilterTrigger(object sender, EventArgs e)
		{
			filterTriggerTimer.Stop();
			filterText = settingFilterText;

			Log.Debug($"Filter: {filterText}");

			CommitViewModel selectedBefore = (CommitViewModel)SelectedItem;
			int indexBefore = Commits.FindIndex(c => c == selectedBefore);

			viewModelService.SetFilter(this, filterText);
			int indexAfter = Commits.FindIndex(c => c == selectedBefore);

			Log.Debug($"Selected {indexBefore}->{indexAfter} for commit {selectedBefore}");
			ScrollRows(indexBefore - indexAfter);

			VirtualItemsSource.DataChanged(width);
		}


		public void Clicked(int column, int rowIndex, bool isControl)
		{
			if (rowIndex < 0 || rowIndex >= Commits.Count || column < 0 || column >= Branches.Count)
			{
				// Click is not within supported area
				return;
			}

			CommitViewModel commitViewModel = Commits[rowIndex];

			if (commitViewModel.IsMergePoint && commitViewModel.BranchColumn == column)
			{
				// User clicked on a merge point (toggle between expanded and collapsed)
				int rowsChange = viewModelService.ToggleMergePoint(this, commitViewModel.Commit);

				ScrollRows(rowsChange);
				VirtualItemsSource.DataChanged(width);
			}
		}


		private void ShowBranch(Branch branch)
		{
			viewModelService.ShowBranch(this, branch);
		}


		private void HideBranch(string obj)
		{
			throw new System.NotImplementedException();
		}


		private void ToggleDetails()
		{
			DetailsSize = DetailsSize > 0 ? 0 : 150;
		}


		public void Clicked(Point position, bool isControl)
		{
			double xpos = position.X - 9;
			double ypos = position.Y - 5;

			int column = Converter.ToColumn(xpos);
			int x = Converter.ToX(column);

			int row = Converter.ToRow(ypos);
			int y = Converter.ToY(row) + 10;

			double absx = Math.Abs(xpos - x);
			double absy = Math.Abs(ypos - y);

			if ((absx < 10) && (absy < 10))
			{
				Clicked(column, row, isControl);
				
			}
		}
	}
}