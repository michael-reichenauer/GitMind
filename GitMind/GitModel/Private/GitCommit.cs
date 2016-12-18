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
			string sha,
			string subject,
			string author,
			DateTime authorDate,
			DateTime commitDate,
			List<CommitId> parents)
		{
			Sha = sha;
			Subject = subject;
			Author = author;
			AuthorDate = authorDate;
			CommitDate = commitDate;
			Parents = parents;
		}

		[DataMember] public string Sha { get; private set; }
		[DataMember] public string Subject { get; private set; }
		[DataMember] public string Author { get; private set; }
		[DataMember] public DateTime AuthorDate { get; private set; }
		[DataMember] public DateTime CommitDate { get; private set; }
		[DataMember] public List<CommitId> Parents { get; private set; }

		[DataMember] public string BranchName { get; private set; }
		[DataMember] public string BranchNameFromSubject { get; private set; }

		public void SetBranchName(string banchName) => BranchName = banchName;
		public void SetBranchNameFromSubject(string banchName) => BranchNameFromSubject = banchName;

		public override string ToString() => $"{Sha.Substring(0, 6)} {AuthorDate} {Subject}";
	}
}