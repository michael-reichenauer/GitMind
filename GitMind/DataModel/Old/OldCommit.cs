using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.DataModel.Private;


namespace GitMind.DataModel.Old
{
	internal class OldCommit
	{
		private static readonly Lazy<IReadOnlyList<OldCommit>> NoCommits =
			new Lazy<IReadOnlyList<OldCommit>>(() => new OldCommit[0]);
	
		public static OldCommit None = new OldCommit(
			"000000", NoCommits, NoCommits, "", "", DateTime.MinValue, DateTime.MinValue, null);

		private readonly Lazy<IReadOnlyList<OldCommit>> parents;
		private readonly Lazy<IReadOnlyList<OldCommit>> children;
		private readonly Lazy<MergeBranchNames> branchNamesFromSubject;
		private readonly Lazy<string> branchNameFromSubject;

		public OldCommit(
			string id,
			Lazy<IReadOnlyList<OldCommit>> parents,
			Lazy<IReadOnlyList<OldCommit>> children,
			string subject,
			string author,
			DateTime dateTime,
			DateTime commitDateTime,
			string branchName)
		{
			Id = id;
			this.children = children;
			this.parents = parents;
			ShortId = id.Length > 6 ? id.Substring(0, 6) : id;
			Subject = subject;
			Author = author;
			DateTime = dateTime;
			CommitDateTime = commitDateTime;
			BranchName = branchName;

			branchNamesFromSubject = new Lazy<MergeBranchNames>(ParseBranchNamesFromSubject);
			branchNameFromSubject = new Lazy<string>(TryExtractBranchNameFromSubject);
		}


		private MergeBranchNames ParseBranchNamesFromSubject()
		{
			if (SecondParent == OldCommit.None)
			{
				// This is no merge commit, i.e. no branch names to parse
				return BranchNameParser.NoMerge;
			}

			return BranchNameParser.ParseBranchNamesFromSubject(this.Subject);
		}


		public string Id { get; }
		public string ShortId { get; }

		public IReadOnlyList<OldCommit> Parents => parents.Value;
		public string Subject { get; }
		public string Author { get; }
		public DateTime DateTime { get; }
		public DateTime CommitDateTime { get; }
		public string BranchName { get; }

		public OldCommit FirstParent => Parents.Any() ? Parents[0] : OldCommit.None;
		public OldCommit SecondParent => Parents.Count > 1 ? Parents[1] : OldCommit.None;

		public OldBranchBuilder Branch { get; set; }
		public OldBranchBuilder ActiveBranch { get; set; }
		public bool IsOnActiveBranch() => ActiveBranch != null;

		public List<OldBranchBuilder> Branches { get; } = new List<OldBranchBuilder>();
		public IReadOnlyList<OldCommit> Children => children.Value;
		public List<OldTag> Tags { get; } = new List<OldTag>();
		public bool IsLocalAheadMarker { get; set; }
		public bool IsRemoteAheadMarker { get; set; }

		public bool IsLocalAhead => IsLocalAheadMarker && !IsSynced;
		public bool IsRemoteAhead => IsRemoteAheadMarker && !IsSynced;
		public bool IsSynced => IsLocalAheadMarker && IsRemoteAheadMarker;
		public bool IsExpanded { get; set; }


		public string TryGetBranchNameFromSubject() => branchNameFromSubject.Value;

		public string TryGetSourceBranchNameFromSubject() =>
			branchNamesFromSubject.Value.SourceBranchName;


		public override string ToString() => $"{ShortId} {DateTime} ({Parents.Count}) {Subject}";


		private string TryExtractBranchNameFromSubject()
		{
			if (SecondParent != OldCommit.None)
			{
				// This is a merge commit, and the subject might contain the target (this current) branch 
				// name in the subject like e.g. "Merge <source-branch> into <target-branch>"
				string branchName = branchNamesFromSubject.Value.TargetBranchName;
				if (branchName != null)
				{
					return branchName;
				}
			}

			// If a child of this commit is a merge commit merged from this commit, lets try to get
			// the source branch name of that commit. I.e. that child commit might have a subject like
			// e.g. "Merge <source-branch> ..." That source branch would thus be the name of the branch
			// of this commit.
			OldCommit childCommit = Children.FirstOrDefault(c => c.SecondParent == this);
			return childCommit?.branchNamesFromSubject.Value.SourceBranchName;
		}
	}
}