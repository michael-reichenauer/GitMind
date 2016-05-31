using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class RepositoryViewModel : ViewModel
	{
		private readonly IViewModelService viewModelService;

		public Repository Repository { get; set; }
		private int width = 0;
		private int graphWidth = 0;

		public List<BranchViewModel> Branches { get; } = new List<BranchViewModel>();
		public List<MergeViewModel> Merges { get; } = new List<MergeViewModel>();
		public List<CommitViewModel> Commits { get; } = new List<CommitViewModel>();
		public Dictionary<string, CommitViewModel> CommitsById { get; } =
			new Dictionary<string, CommitViewModel>();


		public RepositoryViewModel()
			: this(new ViewModelService())
		{		
		}

		public RepositoryViewModel(IViewModelService viewModelService)
		{
			this.viewModelService = viewModelService;
			ItemsSource = new RepositoryVirtualItemsSource(Branches, Merges, Commits);
		}


		public void Update(Repository repository)
		{
			viewModelService.Update(this, repository);
		}


		public ICommand ShowBranchCommand => Command<string>(ShowBranch);
		public ICommand HideBranchCommand => Command<string>(HideBranch);
		public ICommand ToggleDetailsCommand => Command(ToggleDetails);


	

		public RepositoryVirtualItemsSource ItemsSource { get; }

		public ObservableCollection<Branch> ActiveBranches { get; }
			= new ObservableCollection<Branch>();


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


		public int SelectedIndex
		{
			get { return Get(); }
			set
			{
				Log.Debug($"Setting value {value}");
				CommitViewModel commit = Commits[value];

				CommitDetail.Id = commit.Id;
				CommitDetail.Branch = commit.Commit.Branch.Name;
				CommitDetail.Tickets = commit.Tickets;
				CommitDetail.Tags = commit.Tags;
				CommitDetail.Subject = commit.Subject;
			}
		}



		private void ShowBranch(string obj)
		{
			throw new System.NotImplementedException();
		}


		private void HideBranch(string obj)
		{
			throw new System.NotImplementedException();
		}


		private void ToggleDetails()
		{
			DetailsSize = DetailsSize > 0 ? 0 : 150;
		}



	}
}