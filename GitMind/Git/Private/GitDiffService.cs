using System.IO;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitDiffService : IGitDiffService
	{
		private readonly IRepoCaller repoCaller;
		private readonly IGitDiffParser gitDiffParser;


		public GitDiffService()
			: this(new RepoCaller(), new GitDiffParser())
		{			
		}


		public GitDiffService(
			IRepoCaller repoCaller,
			IGitDiffParser gitDiffParser)
		{
			this.repoCaller = repoCaller;
			this.gitDiffParser = gitDiffParser;
		}
		 

		public Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string path)
		{
			Log.Debug($"Get diff for file {path} for commit {commitId} ...");
			return repoCaller.UseRepoAsync(workingFolder, async repo =>
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


		public Task<R<CommitDiff>> GetCommitDiffAsync(string workingFolder, string commitId)
		{
			Log.Debug($"Get diff for commit {commitId} ...");
			return repoCaller.UseRepoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetPatch(commitId);

				return await gitDiffParser.ParseAsync(commitId, patch);
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffRangeAsync(string workingFolder, string id1, string id2)
		{
			Log.Debug($"Get diff for commit range {id1}-{id2} ...");
			return repoCaller.UseRepoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetPatchRange(id1, id2);

				return await gitDiffParser.ParseAsync(null, patch);
			});
		}


		public void GetFile(string workingFolder, string fileId, string filePath)
		{
			Log.Debug($"Get file {fileId}, {filePath} ...");
			repoCaller.UseRepo(workingFolder, repo => repo.GetFile(fileId, filePath));
		}


		public Task ResolveAsync(string workingFolder, string path)
		{
			Log.Debug($"Resolve {path}  ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.Resolve(path));
		}

	}
}