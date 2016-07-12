using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;


namespace GitMind.Git
{
	internal class GitRepository : IDisposable
	{
		private readonly Repository repository;


		public GitRepository(Repository repository)
		{
			this.repository = repository;
		}


		public IEnumerable<GitBranch> Branches => repository.Branches.Select(b => new GitBranch(b));

		public IEnumerable<GitTag> Tags => repository.Tags.Select(t => new GitTag(t));

		public GitBranch Head => new GitBranch(repository.Head);

		public GitStatus Status => new GitStatus(repository.RetrieveStatus(new StatusOptions()));

		public GitDiff Diff => new GitDiff(repository.Diff, repository);


		public void Dispose()
		{
			repository.Dispose();
		}


		public void Fetch()
		{
			repository.Fetch("origin");
		}
	}
}