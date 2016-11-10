using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Branches.Private;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Features.Remote.Private
{
	internal class RemoteService : IRemoteService
	{
		private readonly Lazy<IRepositoryCommands> lazyRepositoryCommands;
		private readonly IProgressService progress;
		private readonly IMessage message;
		private readonly INetworkService networkService;
		private readonly IGitBranchService gitBranchService;
		private IRepositoryCommands repositoryCommands => lazyRepositoryCommands.Value;


		public RemoteService(
			Lazy<IRepositoryCommands> repositoryCommands,
			IProgressService progress,
			IMessage message,
			INetworkService networkService,
			IGitBranchService gitBranchService)
		{
			this.lazyRepositoryCommands = repositoryCommands;
			this.progress = progress;
			this.message = message;
			this.networkService = networkService;
			this.gitBranchService = gitBranchService;
		}


		public bool CanExecuteTryUpdateAllBranches()
		{
			return repositoryCommands.Repository?.Branches.Any(b => b.CanBeUpdated) ?? false;
		}


		public void TryUpdateAllBranches()
		{
			Log.Debug("Try update all branches");

			using (repositoryCommands.DisableStatus())
			{
				progress.Show("Update all branches ...", async state =>
				{
					Branch currentBranch = repositoryCommands.Repository.CurrentBranch;

					R result = await networkService.FetchAsync();

					if (result.IsOk && currentBranch.CanBeUpdated)
					{
						state.SetText($"Update current branch {currentBranch.Name} ...");
						result = await gitBranchService.MergeCurrentBranchAsync();
					}

					if (result.IsFaulted)
					{
						message.ShowWarning(
							$"Failed to update current branch {currentBranch.Name}\n{result.Error.Exception.Message}.");
					}

					IEnumerable<Branch> updatableBranches = repositoryCommands.Repository.Branches
						.Where(b => !b.IsCurrentBranch && b.CanBeUpdated)
						.ToList();

					foreach (Branch branch in updatableBranches)
					{
						state.SetText($"Update branch {branch.Name} ...");

						await networkService.FetchBranchAsync(branch.Name);
					}

					state.SetText("Update all branches ...");
					await networkService.FetchAllNotesAsync();

					state.SetText($"Update status after update all branches ...");
					await repositoryCommands.RefreshAfterCommandAsync(false);
				});
			}
		}
	}
}