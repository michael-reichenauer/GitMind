using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Features.StatusHandling;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.MainWindowViews;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.Features.Tags.Private
{
	internal class TagService : ITagService
	{
		private readonly IStatusService statusService;
		private readonly IProgressService progress;
		private readonly IGitPushService gitPushService;
		private readonly IGitTagService gitTagService;
		private readonly IMessage message;
		private readonly WindowOwner owner;


		public TagService(
			IStatusService statusService,
			IProgressService progressService,
			IGitPushService gitPushService,
			IGitTagService gitTagService,
			IMessage message,
			WindowOwner owner)
		{
			this.statusService = statusService;
			this.progress = progressService;
			this.gitPushService = gitPushService;
			this.gitTagService = gitTagService;
			this.message = message;
			this.owner = owner;
		}


		public async Task CopyTagsAsync(MRepository repository)
		{
			R<IReadOnlyList<GitTag>> tags = await gitTagService.GetAllTagsAsync(CancellationToken.None);

			if (tags.IsFaulted)
			{
				Log.Warn($"Failed to copy tags to repository\n{tags}");
				return;
			}

			foreach (var tag in tags.Value)
			{
				if (repository.Commits.TryGetValue(new CommitId(tag.CommitId), out MCommit commit))
				{
					string name = tag.TagName;
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
		}


		public async Task AddTagAsync(CommitSha commitSha)
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
						R result = await gitTagService.AddTagAsync(commitSha.Sha, tagText, CancellationToken.None);
						if (result.IsOk)
						{
							// Try to push immediately
							Log.Debug($"Try to push tag: '{tagText}'");
							R pushResult = await gitPushService.PushTagAsync(tagText, CancellationToken.None);
							if (pushResult.IsFaulted)
							{
								message.ShowWarning($"Failed to push tag '{tagText}'\n{pushResult.AllMessages}");
							}
						}

						if (result.IsFaulted)
						{
							message.ShowWarning($"Failed to add tag '{tagText}'\n{result.AllMessages}");
						}
					}
				}
			}
		}


		public async Task DeleteTagAsync(string tagName)
		{
			Log.Debug($"Delete tag {tagName}");
			using (statusService.PauseStatusNotifications(Refresh.Repo))
			{
				using (progress.ShowDialog($"Delete tag {tagName} ..."))
				{
					R deleteLocalResult = await gitTagService.DeleteTagAsync(tagName, CancellationToken.None);

					R result = deleteLocalResult;
					if (deleteLocalResult.IsOk)
					{
						// Try to delete remote
						result = await gitPushService.PushDeleteRemoteTagAsync(tagName, CancellationToken.None);
					}

					if (result.IsFaulted)
					{
						message.ShowWarning($"Failed to delete tag '{tagName}'\n{result.AllMessages}");
					}
				}
			}
		}
	}
}