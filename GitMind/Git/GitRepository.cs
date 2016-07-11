using System;
using System.Collections.Generic;
using System.Linq;



namespace GitMind.Git
{
	internal class GitRepository : IDisposable
	{
		private readonly LibGit2Sharp.Repository repository;


		public GitRepository(LibGit2Sharp.Repository repository)
		{
			this.repository = repository;
		}


		public IEnumerable<GitBranch> Branches => repository.Branches.Select(b => new GitBranch(b));

		public IEnumerable<GitTag> Tags => 
			repository.Tags.Select(t => new GitTag(t.Target.Sha, t.FriendlyName));

		public GitBranch Head => new GitBranch(repository.Head);


		public void Dispose()
		{
			repository.Dispose();
		}
	}
}