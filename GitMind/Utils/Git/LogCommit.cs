using System;
using System.Collections.Generic;


namespace GitMind.Utils.Git
{
	public class LogCommit
	{
		public string Sha { get; }
		public string Subject { get; }
		public string Message { get; }
		public string Author { get; }
		public DateTime AuthorDate { get; }
		public DateTime CommitDate { get; }
		public IReadOnlyList<string> ParentIds { get; }


		public LogCommit(
			string sha,
			string subject,
			string message,
			string author,
			DateTime authorDate,
			DateTime commitDate,
			IReadOnlyList<string> parentIds)
		{
			Sha = sha;
			Subject = subject;
			Message = message;
			Author = author;
			AuthorDate = authorDate;
			CommitDate = commitDate;
			ParentIds = parentIds;
		}

		public override string ToString() => $"{Sha.Substring(0, 6)} {CommitDate} {Message}";
	}
}