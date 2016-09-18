using GitMind.GitModel.Private;
using ProtoBuf.Meta;


namespace GitMind.Utils
{
	public static class MySerializer
	{
		public static void Init()
		{
			RegisterMBranch();
			RegisterMCommit();
			RegisterMRepository();
		}


		private static void RegisterMRepository()
		{
			RuntimeTypeModel.Default.Add(typeof(MRepository), false)
				.Add(1, nameof(MRepository.Version))
				.Add(2, nameof(MRepository.CurrentCommitId))
				.Add(3, nameof(MRepository.CurrentBranchId))
				.Add(4, nameof(MRepository.Commits))
				.Add(5, nameof(MRepository.Branches))
				.Add(6, nameof(MRepository.ChildrenById))
				.Add(7, nameof(MRepository.FirstChildrenById));
		}


		private static void RegisterMCommit()
		{
			RuntimeTypeModel.Default.Add(typeof(MCommit), false)
				.Add(1, nameof(MCommit.Id))
				.Add(2, nameof(MCommit.BranchId))
				.Add(3, nameof(MCommit.ShortId))
				.Add(4, nameof(MCommit.Subject))
				.Add(5, nameof(MCommit.Author))
				.Add(6, nameof(MCommit.AuthorDate))
				.Add(7, nameof(MCommit.CommitDate))
				.Add(8, nameof(MCommit.ParentIds))
				.Add(9, nameof(MCommit.BranchName))
				.Add(10, nameof(MCommit.SpecifiedBranchName))
				.Add(11, nameof(MCommit.IsLocalAheadMarker))
				.Add(12, nameof(MCommit.IsRemoteAheadMarker))
				.Add(13, nameof(MCommit.Tags))
				.Add(14, nameof(MCommit.Tickets))
				.Add(15, nameof(MCommit.IsVirtual))
				.Add(16, nameof(MCommit.BranchTips))
				.Add(17, nameof(MCommit.CommitId));
		}


		private static void RegisterMBranch()
		{
			RuntimeTypeModel.Default.Add(typeof(MBranch), false)
				.Add(1, nameof(MBranch.Id))
				.Add(2, nameof(MBranch.Name))
				.Add(3, nameof(MBranch.TipCommitId))
				.Add(4, nameof(MBranch.FirstCommitId))
				.Add(5, nameof(MBranch.ParentCommitId))
				.Add(6, nameof(MBranch.ParentBranchId))
				.Add(7, nameof(MBranch.IsMultiBranch))
				.Add(8, nameof(MBranch.IsActive))
				.Add(9, nameof(MBranch.IsCurrent))
				.Add(10, nameof(MBranch.IsDetached))
				.Add(11, nameof(MBranch.IsLocal))
				.Add(12, nameof(MBranch.IsRemote))
				.Add(13, nameof(MBranch.LocalAheadCount))
				.Add(14, nameof(MBranch.RemoteAheadCount))
				.Add(15, nameof(MBranch.IsLocalAndRemote))
				.Add(16, nameof(MBranch.ChildBranchNames))
				.Add(17, nameof(MBranch.CommitIds));
		}
	}
}