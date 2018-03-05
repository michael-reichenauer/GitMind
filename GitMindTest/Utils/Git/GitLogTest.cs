using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitLogTest : GitTestBase<IGitLog>
	{
		[Test, Explicit]
		public async Task Test()
		{
			IReadOnlyList<LogCommit> result = await gitCmd.GetAsync(ct);

			Log.Debug($"Log contained {result.Count} Commits");
		}
	}
}