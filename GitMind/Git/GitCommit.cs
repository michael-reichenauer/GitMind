using System;
using System.Collections.Generic;


namespace GitMind.Git
{
	public class GitCommit
	{
		public static readonly GitCommit None = new GitCommit(
			"000000", "", "", new string[0], DateTime.MinValue, DateTime.MinValue, null);

		public GitCommit(
			string id,
			string subject,
			string author,
			IReadOnlyList<string> parentIds,
			DateTime dateTime,
			DateTime commitDate,
			string branchName)
		{
			Id = id;
			ShortId = id.Length > 6 ? id.Substring(0, 6) : id;
			Subject = subject;
			Author = author;
			ParentIds = parentIds;
			DateTime = dateTime;
			CommitDate = commitDate;
			BranchName = branchName;
		}


		public string Id { get; }
		public string ShortId { get; }
		public string Author { get; }
		public IReadOnlyList<string> ParentIds { get; }
		public DateTime DateTime { get; }
		public DateTime CommitDate { get; }
		public string BranchName { get; }
		public string Subject { get; }

		public override string ToString() => $"{ShortId} {DateTime} ({ParentIds.Count}) {Subject}";
	}
}