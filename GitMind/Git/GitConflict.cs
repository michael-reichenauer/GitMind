namespace GitMind.Git
{
	public class GitConflict
	{
		public string Path { get; }
		public string OursId { get; }
		public string TheirsId { get;  }
		public string BaseId { get; }


		public GitConflict(string path, string oursId, string theirsId, string baseId)
		{
			Path = path;
			OursId = oursId;
			TheirsId = theirsId;
			BaseId = baseId;
		}

		public bool IsBothModified => OursId != null && TheirsId != null && BaseId != null;
		public bool IsTheirsDeleted => OursId != null && TheirsId == null;
		public bool IsOursDeleted => OursId == null && TheirsId != null;
	}
}