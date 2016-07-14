using System;
using System.Collections.Generic;
using System.Linq;


namespace GitMind.Git
{
	public class GitCommit
	{
		public static readonly string UncommittedId = new string('0', 40);

		private readonly LibGit2Sharp.Commit commit;


		public GitCommit(LibGit2Sharp.Commit commit)
		{
			this.commit = commit;
			ShortId = Id.Substring(0, 6);
		}


		public string Id => commit.Sha;
		public string ShortId { get; }
		public string Author => commit.Author.Name;

		public DateTime AuthorDate => commit.Author.When.LocalDateTime;
		public DateTime CommitDate => commit.Committer.When.LocalDateTime;
		public string Subject => commit.MessageShort;
		public IEnumerable<GitCommit> Parents => commit.Parents.Select(p => new GitCommit(p));

		public override string ToString() => $"{ShortId} {AuthorDate} {Subject}";
	}
}