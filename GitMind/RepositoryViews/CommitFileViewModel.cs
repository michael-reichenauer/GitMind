using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitFileViewModel : ViewModel
	{
		private readonly IDiffService diffService = new DiffService();


		public CommitFileViewModel(Command<string> undoUncommittedFileCommand)
		{
			UndoUncommittedFileCommand = undoUncommittedFileCommand.With(() => Name);
		}

		public string Id { get; set; }

		public string WorkingFolder { get; set; }

		public string Name
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Status
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool IsUncommitted => Id == Commit.UncommittedId;

		public Command ShowDiffCommand => Command(
			() => diffService.ShowFileDiffAsync(WorkingFolder, Id, Name));

		public Command UndoUncommittedFileCommand { get; }
	}
}