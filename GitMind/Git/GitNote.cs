namespace GitMind.Git
{
	internal class GitNote
	{
		public GitNote(string nameSpace, string message)
		{
			NameSpace = nameSpace;
			Message = message;
		}

		public string NameSpace { get; }
		public string Message { get;  }
	}
}