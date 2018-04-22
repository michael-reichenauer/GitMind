using System.Windows.Media;
using GitMind.Common;
using GitMind.Common.ThemeHandling;
using GitMind.Features.Diffing;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitFileViewModel : ViewModel
	{
		private readonly IDiffService diffService;
		private readonly IThemeService themeService;

		private readonly CommitFile file;

		public CommitFileViewModel(
			IDiffService diffService,
			IThemeService themeService,
			CommitFile file,
			Command<string> undoUncommittedFileCommand)
		{
			this.diffService = diffService;
			this.themeService = themeService;
			this.file = file;
			UndoUncommittedFileCommand = undoUncommittedFileCommand.With(() => Name);
		}


		public CommitSha Id { get; set; }

		public string WorkingFolder { get; set; }

		public string Name { get => Get(); set => Set(value); }

		public string Status { get => Get(); set => Set(value); }

		public bool HasConflicts => file.Status.HasFlag(GitFileStatus.Conflict);
		public bool HasNotConflicts => !HasConflicts;


		public Brush FileNameBrush => file.Status.HasFlag(GitFileStatus.Conflict)
			? themeService.Theme.ConflictBrush : themeService.Theme.TextBrush;

		public bool IsUncommitted => HasNotConflicts && Id == CommitSha.Uncommitted;

		public Command ShowDiffCommand => Command(
			() => diffService.ShowFileDiffAsync(Id, Name));

		public Command DefaultCommand => Command(
			() =>
			{
				if (diffService.CanMergeConflict(file))
				{
					diffService.MergeConflictsAsync(Id, file);
				}
				else if (!HasConflicts)
				{
					diffService.ShowFileDiffAsync(Id, Name);
				}
			});

		public Command UndoUncommittedFileCommand { get; }

		public Command MergeConflictsCommand => AsyncCommand(
			() => diffService.MergeConflictsAsync(Id, file),
			() => diffService.CanMergeConflict(file));

		//public Command ResolveCommand => AsyncCommand(
		//	() => diffService.ResolveAsync(WorkingFolder, file),
		//	() => diffService.CanResolve(WorkingFolder, file));

		public Command UseYoursCommand => AsyncCommand(
			() => diffService.UseYoursAsync(file),
			() => diffService.CanUseYours(file));

		public Command UseTheirsCommand => AsyncCommand(
			() => diffService.UseTheirsAsync(file),
			() => diffService.CanUseTheirs(file));

		public Command UseBaseCommand => AsyncCommand(
			() => diffService.UseBaseAsync(file),
			() => diffService.CanUseBase(file));

		public Command DeleteConflictCommand => AsyncCommand(
			() => diffService.DeleteAsync(file),
			() => diffService.CanDelete(file));

		public Command ShowYourDiffCommand => AsyncCommand(
			() => diffService.ShowYourDiffAsync(file),
			() => diffService.CanUseYours(file));

		public Command ShowTheirDiffCommand => AsyncCommand(
			() => diffService.ShowTheirDiffAsync(file),
			() => diffService.CanUseTheirs(file));

	}
}