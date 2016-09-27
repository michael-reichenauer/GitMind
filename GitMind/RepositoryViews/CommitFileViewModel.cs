using System.Threading.Tasks;
using System.Windows.Media;
using GitMind.Features.Committing;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitFileViewModel : ViewModel
	{
		private readonly IDiffService diffService = new DiffService();

		private readonly CommitFile file;



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

		public bool HasConflicts => file.Status.HasFlag(GitFileStatus.Conflict);
		public bool HasNotConflicts => !HasConflicts;
		//public bool IsMerged => diffService.IsMerged(WorkingFolder, file);
		//public bool IsDeleted => diffService.IsDeleted(WorkingFolder, file);
		//public bool IsUseBase => diffService.IsUseBase(WorkingFolder, file);
		//public bool IsUseYours => diffService.IsUseYours(WorkingFolder, file);
		//public bool IsUseTheirs => diffService.IsUseTheirs(WorkingFolder, file);

		public Brush FileNameBrush => file.Status != GitFileStatus.Conflict 
			? BrushService.TextBrush : BrushService.ConflictBrush;

		public bool IsUncommitted => HasNotConflicts && Id == Commit.UncommittedId;

		public Command ShowDiffCommand => Command(
			() => diffService.ShowFileDiffAsync(WorkingFolder, Id, Name));

		public Command DefaultCommand => Command(
			() =>
			{
				if (diffService.CanMergeConflict(file))
				{
					diffService.MergeConflictsAsync(WorkingFolder, Id, file);
				}
				else if (!HasConflicts)
				{
					diffService.ShowFileDiffAsync(WorkingFolder, Id, Name);
				}
			});

		public Command UndoUncommittedFileCommand { get; }

		public Command MergeConflictsCommand => AsyncCommand(
			() => diffService.MergeConflictsAsync(WorkingFolder, Id, file),
			() => diffService.CanMergeConflict(file));

		//public Command ResolveCommand => AsyncCommand(
		//	() => diffService.ResolveAsync(WorkingFolder, file),
		//	() => diffService.CanResolve(WorkingFolder, file));

		public Command UseYoursCommand => AsyncCommand(
			() => diffService.UseYoursAsync(WorkingFolder, file),
			() => diffService.CanUseYours(file));

		public Command UseTheirsCommand => AsyncCommand(
			() => diffService.UseTheirsAsync(WorkingFolder, file),
			() => diffService.CanUseTheirs(file));

		public Command UseBaseCommand => AsyncCommand(
			() => diffService.UseBaseAsync(WorkingFolder, file),
			() => diffService.CanUseBase(WorkingFolder, file));

		public Command DeleteConflictCommand => AsyncCommand(
			() => diffService.DeleteAsync(WorkingFolder, file),
			() => diffService.CanDelete(WorkingFolder, file));

		public Command ShowYourDiffCommand => AsyncCommand(
			() => diffService.ShowYourDiffAsync(WorkingFolder, file),
			() => diffService.CanUseYours(file));

		public Command ShowTheirDiffCommand => AsyncCommand(
			() => diffService.ShowTheirDiffAsync(WorkingFolder, file),
			() => diffService.CanUseTheirs(file));

	}
}