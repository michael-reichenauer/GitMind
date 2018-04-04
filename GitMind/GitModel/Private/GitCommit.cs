using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitMind.Common;


namespace GitMind.GitModel.Private
{
	[DataContract]
	public class GitCommit
	{
		private GitCommit()
		{			
		}

		public GitCommit(
			CommitSha sha,
			string subject,
			string message,
			string author,
			DateTime authorDate,
			DateTime commitDate,
			List<CommitId> parentIds)
		{
			Sha = sha;
			Subject = subject;
			Message = message;
			Author = author;
			AuthorDate = authorDate;
			CommitDate = commitDate;
			ParentIds = parentIds;
		}
		[DataMember] public CommitSha Sha { get; private set; }
		[DataMember] public string Subject { get; private set; }
		[DataMember] public string Message { get; private set; }
		[DataMember] public string Author { get; private set; }
		[DataMember] public DateTime AuthorDate { get; private set; }
		[DataMember] public DateTime CommitDate { get; private set; }
		[DataMember] public List<CommitId> ParentIds { get; private set; } = new List<CommitId>();

		[DataMember] public string BranchNameFromSubject { get; private set; }

		public void SetBranchNameFromSubject(string banchName) => BranchNameFromSubject = banchName;

		public override string ToString() => $"{Sha.ShortSha} {AuthorDate} {Subject}";
	}
}