using ProtoBuf;


namespace GitMind.GitModel
{
	[ProtoContract]
	public class CommitFile
	{
		[ProtoMember(1)]
		public string Name { get; set; }
		[ProtoMember(2)]
		public string Status { get; set; }


		public CommitFile()
		{		
		}

		public CommitFile(string name, string status)
		{
			Name = name;
			Status = status;
		}
	}
}