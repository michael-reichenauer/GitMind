﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel.Private;


namespace GitMind.Utils.Git
{
	public interface IGitCommitService
	{
		Task<R<IReadOnlyList<GitFile>>> GetCommitFilesAsync(
			string sha, CancellationToken ct);


		Task<R<GitCommit>> CommitAllChangesAsync(string message, CancellationToken ct);
		Task<R<GitCommit>> GetCommitAsync(string sha, CancellationToken ct);

		Task<R> UndoCommitAsync(string sha, CancellationToken ct);
		Task<R> UnCommitAsync(CancellationToken ct);
		Task<R<string>> GetCommitMessageAsync(string sha, CancellationToken ct);
	}
}