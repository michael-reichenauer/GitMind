﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitFetch
	{
		Task<GitResult> FetchAsync(CancellationToken ct);
		Task<GitResult> FetchBranchAsync(string branchName, CancellationToken ct);
		Task<GitResult> FetchRefsAsync(IEnumerable<string> refspecs, CancellationToken ct);
		Task<GitResult> FetchPruneTagsAsync(CancellationToken ct);
	}
}