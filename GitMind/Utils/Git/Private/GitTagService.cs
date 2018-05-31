using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitTagService : IGitTagService
	{
		private readonly IGitCmdService gitCmdService;


		public GitTagService(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		public async Task<R<IReadOnlyList<GitTag>>> GetAllTagsAsync(CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync("show-ref -d --tags", ct);
			if (result.IsFaulted)
			{
				if (!(result.ExitCode == 1 && string.IsNullOrEmpty(result.Output)))
				{
					return R.Error("Failed to list tags", result.AsException());
				}
			}

			IReadOnlyList<GitTag> tags = ParseTags(result);

			Log.Info($"Got {tags.Count} tags");
			return R.From(tags);
		}


		public async Task<R<GitTag>> AddTagAsync(string sha, string tagName, CancellationToken ct)
		{
			CmdResult2 result = await gitCmdService.RunCmdAsync($"tag {tagName} {sha}", ct);
			if (result.IsFaulted)
			{
				return R.Error($"Failed to add tag {tagName} at {sha}", result.AsException());
			}

			Log.Info($"Added {tagName} at {sha}");
			return new GitTag(sha, tagName);
		}


		public async Task<R> DeleteTagAsync(string tagName, CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync($"tag --delete {tagName}", ct);
			if (result.IsFaulted)
			{
				return R.Error($"Failed to delete tag {tagName}", result.Exception);
			}

			Log.Info($"Deleted {tagName}");

			return R.Ok;
		}


		private IReadOnlyList<GitTag> ParseTags(R<CmdResult2> result)
		{
			List<GitTag> tags = new List<GitTag>();
			foreach (string line in result.Value.OutputLines)
			{
				string sha = line.Substring(0, 40);
				string tagName = line.Substring(51);
				tags.Add(new GitTag(sha, tagName));
			}

			return tags;
		}
	}
}