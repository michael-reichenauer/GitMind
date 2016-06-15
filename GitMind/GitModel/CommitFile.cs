using ProtoBuf;


namespace GitMind.GitModel
{
	[ProtoContract]
	public class CommitFile
	{
		//private readonly IDiffService diffService = new DiffService();

		[ProtoMember(1)]
		public string Id { get; set; }
		[ProtoMember(2)]
		public string Name { get; set; }
		[ProtoMember(3)]
		public string Status { get; set; }


		public CommitFile()
		{		
		}

		public CommitFile(string id, string name, string status)
		{
			Id = id;
			Name = name;
			Status = status;
		}
	}
}