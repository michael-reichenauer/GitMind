using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture, Explicit]
	public class GitCommitTest : GitTestBase<IGitCommit>
	{
		[Test]
		public async Task TestFetch()
		{
			R<IReadOnlyList<CommitFile>> result = await gitCmd.GetCommitFilesAsync("d79878", ct);
			Assert.IsTrue(result.IsOk);
		}
	}
}