using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel.Private;


namespace GitMind.Utils.Git
{
	public interface IGitLogService
	{
		Task<R<IReadOnlyList<GitCommit>>> GetLogAsync(CancellationToken ct);

		Task<R> GetLogAsync(Action<GitCommit> commits, CancellationToken ct);

		Task<R<GitCommit>> GetCommitAsync(string sha, CancellationToken ct);
		Task<R<string>> GetCommitMessageAsync(string sha, CancellationToken ct);
	}
}