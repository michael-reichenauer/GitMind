using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitBranchServiceTest : GitTestBase<IGitBranchService2>
	{
		[Test]
		public async Task TestPusAsync()
		{
			R<IReadOnlyList<GitBranch2>> branches = await gitCmd.GetBranchesAsync(ct);
		}
	}
}