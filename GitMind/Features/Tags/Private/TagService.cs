using System;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Branches.Private;
using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel.Private;
using GitMind.MainWindowViews;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Features.Tags.Private
{
	internal class TagService : ITagService
	{
		private readonly IRepoCaller repoCaller;
		private readonly IStatusService statusService;
		private readonly IProgressService progress;
		private readonly IGitNetworkService gitNetworkService;
		private readonly IMessage message;
		private readonly WindowOwner owner;


		public TagService(
			IRepoCaller repoCaller,
			IStatusService statusService,
			IProgressService progressService,
			IGitNetworkService gitNetworkService,
			IMessage message,
			WindowOwner owner)
		{
			this.repoCaller = repoCaller;
			this.statusService = statusService;
			this.progress = progressService;
			this.gitNetworkService = gitNetworkService;
			this.message = message;
			this.owner = owner;
		}


		public void CopyTags(GitRepository gitRepository, MRepository repository)
		{
			repoCaller.UseLibRepo(repo =>
			{
				foreach (var tag in repo.Tags)
				{
					if (repository.Commits.TryGetValue(new CommitId(tag.Target.Sha), out MCommit commit))
					{
						string name = tag.FriendlyName;
						string tagText = $":{name}:";
						if (commit.Tags != null && -1 == commit.Tags.IndexOf(tagText, StringComparison.Ordinal))
						{
							commit.Tags += tagText;
						}
						else if (commit.Tags == null)
						{
							commit.Tags = tagText;
						}
					}
				}
			});
		}


		public async Task AddTag(CommitSha commitSha)
		{
			using (statusService.PauseStatusNotifications())
			{
				AddTagDialog dialog = new AddTagDialog(owner);

				if (dialog.ShowDialog() == true)
				{
					string tagText = (dialog.TagText ?? "").Trim();
					Log.Debug($"Add tag {tagText}, on {commitSha.ShortSha} ...");

					using (progress.ShowDialog($"Add tag {tagText} ..."))
					{
						R<string> addResult = await repoCaller.UseLibRepoAsync(repository =>
						{
							Commit commit = repository.Lookup<Commit>(new ObjectId(commitSha.Sha));

							Tag tag = repository.Tags.Add(tagText, commit);

							return tag.CanonicalName;
						});

						R result = addResult;
						if (addResult.IsOk)
						{
							// Try to push immediately
							result = await gitNetworkService.PushTagAsync(addResult.Value);
						}
					
						if (result.IsFaulted)
						{
							message.ShowWarning(
								$"Failed to add tag '{tagText}'\n{result.Error.Exception.Message}");
						}
					}
				}
			}
		}
	}
}