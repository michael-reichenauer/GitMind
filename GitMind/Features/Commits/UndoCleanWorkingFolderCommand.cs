using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Git;
using GitMind.RepositoryViews;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.Features.Commits
{
	internal class UndoCleanWorkingFolderCommand : Command
	{
		private readonly WorkingFolder workingFolder;
		private readonly IMessage message;
		private readonly IProgressService progress;
		private readonly Lazy<IRepositoryCommands> repositoryCommands;
		private readonly IGitCommitsService gitCommitsService;


		public UndoCleanWorkingFolderCommand(
			WorkingFolder workingFolder,
			IMessage message,
			IProgressService progressService,
			Lazy<IRepositoryCommands> repositoryCommands,
			IGitCommitsService gitCommitsService)
		{
			this.workingFolder = workingFolder;
			this.message = message;
			this.progress = progressService;
			this.repositoryCommands = repositoryCommands;
			this.gitCommitsService = gitCommitsService;
		}


		protected override Task RunAsync()
		{
			R<IReadOnlyList<string>> failedPaths = R.From(new string[0].AsReadOnlyList());

			using (repositoryCommands.Value.DisableStatus())
			{
				progress.Show($"Undo changes and clean working folder {workingFolder} ...", async () =>
				{
					failedPaths = await gitCommitsService.UndoCleanWorkingFolderAsync();

					await repositoryCommands.Value.RefreshAfterCommandAsync(false);
				});

				if (failedPaths.IsFaulted)
				{
					message.ShowWarning(failedPaths.ToString());
				}
				else if (failedPaths.Value.Any())
				{
					int count = failedPaths.Value.Count;
					string text = $"Failed to undo and clean working folder.\n{count} items where locked:\n";
					foreach (string path in failedPaths.Value.Take(10))
					{
						text += $"\n   {path}";
					}
					if (count > 10)
					{
						text += "   ...";
					}

					message.ShowWarning(text);
				}
			}

			return Task.CompletedTask;
		}
	}
}