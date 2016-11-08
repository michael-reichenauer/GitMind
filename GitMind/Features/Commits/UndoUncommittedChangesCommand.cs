using System;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common.ProgressHandling;
using GitMind.Git;
using GitMind.RepositoryViews;
using GitMind.Utils.UI;


namespace GitMind.Features.Commits
{
	internal class UndoUncommittedChangesCommand : Command
	{
		private readonly WorkingFolder workingFolder;
		private readonly IProgressService progress;
		private readonly Lazy<IRepositoryCommands> repositoryCommands;
		private readonly IGitCommitsService gitCommitsService;


		public UndoUncommittedChangesCommand(
			WorkingFolder workingFolder,
			IProgressService progressService,
			Lazy<IRepositoryCommands> repositoryCommands,
			IGitCommitsService gitCommitsService)
		{
			this.workingFolder = workingFolder;
			this.progress = progressService;
			this.repositoryCommands = repositoryCommands;
			this.gitCommitsService = gitCommitsService;
		}


		protected override Task RunAsync()
		{
			using (repositoryCommands.Value.DisableStatus())
			{
				progress.Show($"Undo changes in working folder {workingFolder} ...", async () =>
				{
					await gitCommitsService.UndoWorkingFolderAsync();

					await repositoryCommands.Value.RefreshAfterCommandAsync(false);
				});
			}

			return Task.CompletedTask;
		}
	}
}