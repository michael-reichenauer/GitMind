namespace GitMind.Git
{
	public class Conflict
	{
		public string Path { get; }
		public string OursId { get; }
		public string TheirsId { get;  }
		public string BaseId { get; }


		public Conflict(string path, string oursId, string theirsId, string baseId)
		{
			Path = path;
			OursId = oursId;
			TheirsId = theirsId;
			BaseId = baseId;
		}
	}


	public class GitFile
	{
		public GitFile(
			string file, 
			string oldFile,
			Conflict conflict,
			bool isModified,
			bool isAdded, 
			bool isDeleted,
			bool isRenamed,
			bool isConflict)
		{
			File = file;
			OldFile = oldFile;
			Conflict = conflict;

			IsModified = isModified;
			IsAdded = isAdded;
			IsDeleted = isDeleted;
			IsRenamed = isRenamed;
			IsConflict = isConflict;
		}

		public string File { get; }
		public string OldFile { get; }
		public Conflict Conflict { get;  }
		public bool IsModified { get; }
		public bool IsAdded { get; }
		public bool IsDeleted { get; }
		public bool IsRenamed { get; }
		public bool IsConflict { get; }

		public override string ToString() => File;
	}
}