using System.Collections.ObjectModel;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;
using GitMind.VirtualCanvas;


namespace GitMind.CommitsHistory
{
	internal class RepositoryViewModel : ViewModel
	{		
		public Repository Repository { get; set; }

		private RepositoryItems repositoryItems;


		public RepositoryViewModel(ICoordinateConverter coordinateConverter)
		{
			repositoryItems = new RepositoryItems(coordinateConverter);

			VirtualItemsSource = new VirtualItemsSource(repositoryItems);
		}


		public ObservableCollection<Branch> ActiveBranches { get; }
			= new ObservableCollection<Branch>();

		public VirtualItemsSource VirtualItemsSource { get; }


		public CommitDetailViewModel CommitDetail { get; } = new CommitDetailViewModel(null);

	


		public int SelectedIndex
		{
			get { return Get(); }
			set
			{
				Log.Debug($"Setting value {value}");
				CommitViewModel commit = repositoryItems.Commits[value];

				CommitDetail.Id = commit.Id;
				CommitDetail.Branch = commit.Commit.Branch.Name;
				CommitDetail.Tickets = commit.Tickets;
				CommitDetail.Tags = commit.Tags;
				CommitDetail.Subject = commit.Subject;
			}
		}
	}
}