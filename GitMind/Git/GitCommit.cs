using System;
using System.Collections.Generic;


namespace GitMind.Git
{
	public class GitCommit
	{
		public static readonly GitCommit None = new GitCommit(
			"000000", "", "", new string[0], DateTime.MinValue, DateTime.MinValue);

		public GitCommit(
			string id,
			string subject,
			string author,
			IReadOnlyList<string> parentIds,
			DateTime authorDate,
			DateTime commitDate)
		{
			Id = id;
			ShortId = id.Length > 6 ? id.Substring(0, 6) : id;
			Subject = subject;
			Author = author;
			ParentIds = parentIds;
			AuthorDate = authorDate;
			CommitDate = commitDate;
		}


		public string Id { get; }
		public string ShortId { get; }
		public string Author { get; }
		public IReadOnlyList<string> ParentIds { get; }
		public DateTime AuthorDate { get; }
		public DateTime CommitDate { get; }
		public string Subject { get; }

		public override string ToString() => $"{ShortId} {AuthorDate} ({ParentIds.Count}) {Subject}";
	}
}