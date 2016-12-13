using System.Collections.Generic;
using System.IO;
using GitMind.Common;
using GitMind.Git;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoSerializer = ProtoBuf.Serializer;


namespace GitMind.GitModel.Private.Caching
{
	public static class Serializer
	{
		static Serializer()
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
			RuntimeTypeModel.Default.Add(typeof(CommitIntBySha), false)
				.SetSurrogate(typeof(CommitIntByShaSurrogate));
			RuntimeTypeModel.Default.Add(typeof(CommitIntByShaSurrogate), true);
		}


		private static void RegisterMRepository()
		{
			RuntimeTypeModel.Default.Add(typeof(MRepository), false)
				.Add(nameof(MRepository.Version))
				.Add(nameof(MRepository.CurrentCommitId))
				.Add(nameof(MRepository.CurrentBranchId))
				.Add(nameof(MRepository.Commits))
				.Add(nameof(MRepository.Branches))
				.Add(nameof(MRepository.TimeToCreateFresh));
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
				.Add(nameof(MCommit.ChildIds))
				.Add(nameof(MCommit.FirstChildIds))
				.Add(nameof(MCommit.BranchName))
				.Add(nameof(MCommit.SpecifiedBranchName))
				.Add(nameof(MCommit.Tags))
				.Add(nameof(MCommit.Tickets))
				.Add(nameof(MCommit.IsVirtual))
				.Add(nameof(MCommit.BranchTips))
				.Add(nameof(MCommit.ViewCommitId))
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

	[ProtoContract]
	internal class CommitIntByShaSurrogate
	{
		[ProtoMember(1)]
		public Dictionary<string, int> CommitIdToInt { get; set; }

		public CommitIntByShaSurrogate(Dictionary<string, int> commitIdToInt)
		{
			CommitIdToInt = commitIdToInt;
		}

		public static implicit operator CommitIntByShaSurrogate(CommitIntBySha commitIntBySha)
		{
			var intByShas = CommitIds.GetIntByShas();

			return new CommitIntByShaSurrogate(intByShas);
		}

		public static implicit operator CommitIntBySha(CommitIntByShaSurrogate commitIntBySha)
		{
			foreach (var pair in commitIntBySha.CommitIdToInt)
			{
				CommitIds.Set(pair.Key, pair.Value);
			}
			return new CommitIntBySha();
		}
	}
}