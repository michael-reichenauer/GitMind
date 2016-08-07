using GitMind.Git;
using ProtoBuf;


namespace GitMind.GitModel
{
	[ProtoContract]
	public class CommitFile
	{
		[ProtoMember(1)]
		public string Path { get; set; }
		[ProtoMember(2)]
		public string Status { get; set; }

		public string OldPath { get; set; }

		public Conflict Conflict { get; set; }


		public CommitFile()
		{		
		}

		public CommitFile(string path, string oldPath, Conflict conflict, string status)
		{
			Path = path;
			OldPath = oldPath;
			Conflict = conflict;
			Status = status;
		}
	}
}