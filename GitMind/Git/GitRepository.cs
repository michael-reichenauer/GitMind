using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;


namespace GitMind.Git
{
	internal class GitRepository : IDisposable
	{
		private readonly Repository repository;
		private static readonly StatusOptions StatusOptions = new StatusOptions
			{ DetectRenamesInWorkDir = true, DetectRenamesInIndex = true };


		public GitRepository(Repository repository)
		{
			this.repository = repository;
		}


		public IEnumerable<GitBranch> Branches => repository.Branches.Select(b => new GitBranch(b));

		public IEnumerable<GitTag> Tags => repository.Tags.Select(t => new GitTag(t));

		public GitBranch Head => new GitBranch(repository.Head);

		public GitStatus Status => new GitStatus(repository.RetrieveStatus(StatusOptions));

		public GitDiff Diff => new GitDiff(repository.Diff, repository);

		public string UserName => repository.Config.GetValueOrDefault<string>("user.name");

		public void Dispose()
		{
			repository.Dispose();
		}


		public void Fetch()
		{
			repository.Fetch("origin");
		}


		public void Commit(string message)
		{
			Signature author = repository.Config.BuildSignature(DateTimeOffset.Now);
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);
			CommitOptions commitOptions = new CommitOptions();

			repository.Commit(message, author, committer, commitOptions);
		}


		public void Add(IReadOnlyList<string> paths)
		{
			repository.Index.Clear();
			paths.ForEach(path => repository.Index.Add(path));
		}
	}
}