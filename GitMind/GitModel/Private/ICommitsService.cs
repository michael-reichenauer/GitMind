using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface ICommitsService
	{
		IReadOnlyList<MCommit> AddCommits(
			IReadOnlyList<GitCommit> gitCommits,
			MRepository repository);


	}
}