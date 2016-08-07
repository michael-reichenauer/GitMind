using System.Threading.Tasks;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitFileViewModel : ViewModel
	{
		private readonly CommitFile file;
		private readonly IDiffService diffService = new DiffService();


		public CommitFileViewModel(CommitFile file, Command<string> undoUncommittedFileCommand)
		{
			this.file = file;
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

		public bool HasConflicts => file.Status == "C";

		public Brush FileNameBrush => file.Status != "C" 
			? BrushService.TextBrush : BrushService.ConflictBrush;

		public bool IsUncommitted => Id == Commit.UncommittedId;

		public Command ShowDiffCommand => Command(
			() => diffService.ShowFileDiffAsync(WorkingFolder, Id, Name));

		public Command UndoUncommittedFileCommand { get; }

		public Command MergeConflictsCommand => AsyncCommand(MergeConflictsAsync);


		public Command ResolveCommand => AsyncCommand(ResolveAsync);


		private Task MergeConflictsAsync()
		{
			return diffService.MergeConflictsAsync(WorkingFolder, Id, file.Path, file.Conflict);
		}

		private Task ResolveAsync()
		{
			return diffService.ResolveAsync(WorkingFolder, file.Path);
		}
	}
}