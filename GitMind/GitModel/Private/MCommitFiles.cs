using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ProtoBuf;


namespace GitMind.GitModel.Private
{
	[ProtoContract]
	public class MCommitFiles
	{
		[DataMember, ProtoMember(1)]
		public string Version { get; set; } = MRepository.CurrentVersion;

		[DataMember, ProtoMember(2)]
		internal IDictionary<string, IList<CommitFile>> CommitsFiles { get; set; }
	}
}