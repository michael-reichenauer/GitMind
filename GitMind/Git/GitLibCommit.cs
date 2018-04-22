using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Common;


namespace GitMind.Git
{
	public class GitLibCommit
	{
		public GitLibCommit(
			CommitSha sha,
			string subject,
			string message,
			string author,
			DateTime authorDate,
			DateTime commitDate,
			List<CommitSha> parentIds)
		{
			Sha = sha;
			Subject = subject;
			Message = message;
			Author = author;
			AuthorDate = authorDate;
			CommitDate = commitDate;
			ParentIds = parentIds;
		}

		public CommitSha Sha { get; }
		public string Subject { get; }
		public string Message { get; }
		public string Author { get; }
		public DateTime AuthorDate { get; }
		public DateTime CommitDate { get; }
		public List<CommitSha> ParentIds { get; }

		public override string ToString() => $"{Sha.ShortSha} {AuthorDate} {Subject}";
	}
}