using System.IO;
using GitMind.Git;
using GitMind.GitModel.Private;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoSerializer = ProtoBuf.Serializer;


namespace GitMind.Utils
{
	public static class Serializer
	{
		public static void RegisterCacheSerializedTypes()
		{
			RegisterMBranch();
			RegisterMCommit();
			RegisterMRepository();
			RegisterBranchName();
		}


		private static void RegisterBranchName()
		{
			RuntimeTypeModel.Default.Add(typeof(BranchName), false)
				.SetSurrogate(typeof(BranchNameSurrogate));
			RuntimeTypeModel.Default.Add(typeof(BranchNameSurrogate), true);
		}


		private static void RegisterMRepository()
		{
			RuntimeTypeModel.Default.Add(typeof(MRepository), false)
				.Add(nameof(MRepository.Version))
				.Add(nameof(MRepository.CurrentCommitId))
				.Add(nameof(MRepository.CurrentBranchId))
				.Add(nameof(MRepository.Commits))
				.Add(nameof(MRepository.Branches))
				.Add(nameof(MRepository.ChildrenById))
				.Add(nameof(MRepository.FirstChildrenById));
		}


		private static void RegisterMCommit()
		{
			RuntimeTypeModel.Default.Add(typeof(MCommit), false)
				.Add(nameof(MCommit.Id))
				.Add(nameof(MCommit.BranchId))
				.Add(nameof(MCommit.ShortId))
				.Add(nameof(MCommit.Subject))
				.Add(nameof(MCommit.Author))
				.Add(nameof(MCommit.AuthorDate))
				.Add(nameof(MCommit.CommitDate))
				.Add(nameof(MCommit.ParentIds))
				.Add(nameof(MCommit.BranchName))
				.Add(nameof(MCommit.SpecifiedBranchName))
				.Add(nameof(MCommit.Tags))
				.Add(nameof(MCommit.Tickets))
				.Add(nameof(MCommit.IsVirtual))
				.Add(nameof(MCommit.BranchTips))
				.Add(nameof(MCommit.CommitId))
				.Add(nameof(MCommit.IsLocalAhead))
				.Add(nameof(MCommit.IsRemoteAhead))
				.Add(nameof(MCommit.IsCommon));
		}


		private static void RegisterMBranch()
		{
			RuntimeTypeModel.Default.Add(typeof(MBranch), false)
				.Add(nameof(MBranch.Id))
				.Add(nameof(MBranch.Name))
				.Add(nameof(MBranch.TipCommitId))
				.Add(nameof(MBranch.FirstCommitId))
				.Add(nameof(MBranch.ParentCommitId))
				.Add(nameof(MBranch.ParentBranchId))
				.Add(nameof(MBranch.IsMultiBranch))
				.Add(nameof(MBranch.IsActive))
				.Add(nameof(MBranch.IsCurrent))
				.Add(nameof(MBranch.IsDetached))
				.Add(nameof(MBranch.IsLocal))
				.Add(nameof(MBranch.IsRemote))
				.Add(nameof(MBranch.LocalAheadCount))
				.Add(nameof(MBranch.RemoteAheadCount))
				.Add(nameof(MBranch.IsLocalAndRemote))
				.Add(nameof(MBranch.LocalTipCommitId))
				.Add(nameof(MBranch.RemoteTipCommitId))
				.Add(nameof(MBranch.IsLocalPart))
				.Add(nameof(MBranch.IsMainPart))
				.Add(nameof(MBranch.MainBranchId))
				.Add(nameof(MBranch.LocalSubBranchId))
				.Add(nameof(MBranch.ChildBranchNames))
				.Add(nameof(MBranch.CommitIds));
		}


		public static void Serialize(FileStream file, object data)
		{
			ProtoSerializer.Serialize(file, data);
		}


		public static T Deserialize<T>(FileStream file)
		{
			return ProtoSerializer.Deserialize<T>(file);
		}


		public static T DeserializeWithLengthPrefix<T>(FileStream file)
		{
			return ProtoSerializer.DeserializeWithLengthPrefix<T>(file, PrefixStyle.Fixed32);
		}
	}

	[ProtoContract]
	internal class BranchNameSurrogate
	{
		[ProtoMember(1)]
		public string Name { get; set; }

		public static implicit operator BranchNameSurrogate(BranchName branchName) =>
			branchName != null ? new BranchNameSurrogate { Name = branchName.ToString() } : null;

		public static implicit operator BranchName(BranchNameSurrogate branchName)=>
			branchName != null ? new BranchName(branchName.Name) : null;
	}
}