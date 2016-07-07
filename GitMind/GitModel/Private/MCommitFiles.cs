using System.Collections.Generic;
using ProtoBuf;


namespace GitMind.GitModel.Private
{
	[ProtoContract]
	public class MCommitFiles
	{
		[ProtoMember(1)]
		public string Version { get; set; } = MRepository.CurrentVersion;

		[ProtoMember(2)]
		internal IDictionary<string, IList<CommitFile>> CommitsFiles { get; set; }
	}
}