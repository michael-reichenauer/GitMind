using GitMind.CommitsHistory;
using GitMind.Utils.UI;


namespace GitMind.GitModel
{
	internal class CommitFile
	{
		//private readonly IDiffService diffService = new DiffService();

		public string Id { get; }
		public string Name { get; }
		public string Status { get; }

		public CommitFile(string id, string name, string status)
		{
			Id = id;
			Name = name;
			Status = status;
		}
	}
}	