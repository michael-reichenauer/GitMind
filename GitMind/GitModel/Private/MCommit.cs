using System.Collections.Generic;
using System.Linq;


namespace GitMind.GitModel.Private
{
	internal class MCommit
	{
		private readonly MModel mModel;

		public MCommit(MModel mModel)
		{
			this.mModel = mModel;
		}


		public string Id { get; set; }
		public string BranchId { get; set; }
		public string ShortId { get; set; }
		public List<string> ParentIds { get; set; } = new List<string>();
		public List<string> ChildIds { get; set; } = new List<string>();
		public List<string> FirstChildIds { get; set; } = new List<string>();

		public bool HasBranchName => !string.IsNullOrEmpty(BranchName);
		public bool HasFirstParent => ParentIds.Count > 0;
		public bool HasSecondParent => ParentIds.Count > 1;
		public bool HasSingleFirstChild => ChildIds.Count == 1;
		public IEnumerable<MCommit> Parents => ParentIds.Select(id => mModel.Commits[id]);
		public IEnumerable<MCommit> Children => ChildIds.Select(id => mModel.Commits[id]);
		public IEnumerable<MCommit> FirstChildren => FirstChildIds.Select(id => mModel.Commits[id]);

		public string branchName;
		public string BranchName
		{
			get { return branchName; }
			set
			{
				branchName = value;

				//if (ShortId == "afe62f")
				//{
				//	Log.Warn($"Setting branch name {branchName} != '{BranchNameFromSubject}' from subject");
				//}
			}
		}
		public string SubBranchId { get; set; }

		public string BranchNameSpecified { get; set; }
		public string BranchNameFromSubject { get; set; }
		public string MergeSourceBranchNameFromSubject { get; set; }
		public string MergeTargetBranchNameFromSubject { get; set; }
		public string Subject { get; set; }
		public string Author { get; set; }
		public string AuthorDate { get; set; }
		public string CommitDate { get; set; }

		public string FirstParentId => ParentIds.Count > 0 ? ParentIds[0] : null;
		public MCommit FirstParent => ParentIds.Count > 0 ? mModel.Commits[ParentIds[0]] : null;
		public string SecondParentId => ParentIds.Count > 1 ? ParentIds[1] : null;
		public MCommit SecondParent => ParentIds.Count > 1 ? mModel.Commits[ParentIds[1]] : null;



		public IEnumerable<MCommit> FirstAncestors()
		{
			MCommit current = FirstParent;
			while (current != null)
			{
				yield return current;
				current = current.FirstParent;
			}
		}

		public override string ToString() => $"{ShortId} {AuthorDate} ({ParentIds.Count}) {Subject} ({CommitDate})";
	}
}