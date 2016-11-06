using GitMind.Features.Diffing;
using LibGit2Sharp;


namespace GitMind.Git
{
	public class GitStatus
	{
		private readonly IDiffService diffService;


		internal GitStatus(
			IDiffService diffService,
			RepositoryStatus status, 
			ConflictCollection conflicts,
			RepositoryInformation info, 
			bool isFullyMerged)
		{
			this.diffService = diffService;
			SetStatus(status, conflicts);
			IsFullyMerged = isFullyMerged;
			CurrentOperation = info.CurrentOperation;
			Message = info.Message;
		}

		public bool OK => Count == 0;
		public string Message { get; }
		public CurrentOperation CurrentOperation { get; }
		public bool IsMerging => CurrentOperation == CurrentOperation.Merge;
		public bool IsFullyMerged { get; }

		public int Count => CommitFiles.Files.Count;
		internal GitCommitFiles CommitFiles { get; private set; }

		public int ConflictCount => ConflictFiles.Files.Count;
		internal GitCommitFiles ConflictFiles { get; private set; }


		private void SetStatus(RepositoryStatus status, ConflictCollection conflicts)
		{
			CommitFiles = new GitCommitFiles(diffService, GitCommit.UncommittedId, status, conflicts);

			ConflictFiles = new GitCommitFiles(diffService, GitCommit.UncommittedId, conflicts);
		}
	}
}