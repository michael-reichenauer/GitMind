﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitConfig
	{
		// bool TryGet(string name, out GitSetting setting);
		Task<R<IReadOnlyList<GitSetting>>> GetAsync(CancellationToken ct);
	}
}