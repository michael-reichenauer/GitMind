using System.Collections.Generic;
using ProtoBuf;


namespace GitMind.GitModel
{
	[ProtoContract]
	public class CommitFiles
	{
		[ProtoMember(1)]
		public string Id { get; set; }
		[ProtoMember(2)]
		public List<CommitFile> Files { get; set; }


		public CommitFiles()
		{
		}

		public CommitFiles(string id, List<CommitFile> files)
		{
			Id = id;
			Files = files;
		}
	}
}