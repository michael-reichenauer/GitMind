using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using GitMind.DataModel.Old;
using GitMind.DataModel.Private;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class CommitDetailViewModel : ViewModel
	{
		private readonly Func<string, Task> showDiffAsync;


		public CommitDetailViewModel(
			Func<string, Task> showDiffAsync)
		{
			this.showDiffAsync = showDiffAsync;

			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
			Files.Add(new CommitFile("file.txt", "1"));
		}

		public OldCommit Commit { get; set; }

		public ObservableCollection<CommitFile> Files { get; }
			= new ObservableCollection<CommitFile>();
	
		public string Id
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Branch
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
			set { Set(value).Notify(nameof(IsTags)); }
		}

		public bool IsTags => !string.IsNullOrWhiteSpace(Tags);


		public string Tickets
		{
			get { return Get(); }
			set { Set(value).Notify(nameof(IsTickets)); }
		}

		public bool IsTickets => !string.IsNullOrWhiteSpace(Tickets);

		public Command ShowDiffCommand => Command(ShowDiffAsync);

		public override string ToString() => $"{Commit.ShortId} {Subject}";


		private async void ShowDiffAsync()
		{
			await showDiffAsync(Id);
		}
	}
}