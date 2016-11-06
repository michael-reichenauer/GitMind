using System.IO;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.Utils;


namespace GitMind.Features.Diffing
{
	internal class GitDiffService : IGitDiffService
	{
		private readonly WorkingFolder workingFolder;
		private readonly IRepoCaller repoCaller;
		private readonly IGitDiffParser gitDiffParser;


		public GitDiffService(
			WorkingFolder workingFolder,
			IRepoCaller repoCaller,
			IGitDiffParser gitDiffParser)
		{
			this.workingFolder = workingFolder;
			this.repoCaller = repoCaller;
			this.gitDiffParser = gitDiffParser;
		}
		 

		public Task<R<CommitDiff>> GetFileDiffAsync(string commitId, string path)
		{
			Log.Debug($"Get diff for file {path} for commit {commitId} ...");
			return repoCaller.UseRepoAsync(async repo =>
			{
				string patch = repo.Diff.GetFilePatch(commitId, path);

				CommitDiff commitDiff = await gitDiffParser.ParseAsync(commitId, patch, false);

				if (commitId == Commit.UncommittedId)
				{
					string filePath = Path.Combine(workingFolder, path);
					if (File.Exists(filePath))
					{
						commitDiff = new CommitDiff(commitDiff.LeftPath, filePath);
					}
				}

				return commitDiff;
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffAsync(string commitId)
		{
			Log.Debug($"Get diff for commit {commitId} ...");
			return repoCaller.UseRepoAsync(async repo =>
			{
				string patch = repo.Diff.GetPatch(commitId);

				return await gitDiffParser.ParseAsync(commitId, patch);
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffRangeAsync(string id1, string id2)
		{
			Log.Debug($"Get diff for commit range {id1}-{id2} ...");
			return repoCaller.UseRepoAsync(async repo =>
			{
				string patch = repo.Diff.GetPatchRange(id1, id2);

				return await gitDiffParser.ParseAsync(null, patch);
			});
		}


		public void GetFile(string fileId, string filePath)
		{
			Log.Debug($"Get file {fileId}, {filePath} ...");
			repoCaller.UseRepo(repo => repo.GetFile(fileId, filePath));
		}


		public Task ResolveAsync(string path)
		{
			Log.Debug($"Resolve {path}  ...");
			return repoCaller.UseRepoAsync(repo => repo.Resolve(path));
		}

	}
}