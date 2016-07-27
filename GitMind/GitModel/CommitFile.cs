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

		public CommitFile()
		{		
		}

		public CommitFile(string path, string oldPath, string status)
		{
			Path = path;
			OldPath = oldPath;
			Status = status;
		}
	}
}