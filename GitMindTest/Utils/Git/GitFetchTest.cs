using System.Threading.Tasks;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture, Explicit]
	public class GitFetchTest : GitTestBase<IGitFetch>
	{
		[Test]
		public async Task TestFetch()
		{
			GitResult result = await gitCmd.FetchAsync(ct);
			Assert.IsTrue(result.IsOk);
		}

		[Test]
		public async Task TestFetchBranch()
		{
			string[] rfs = { "master:master" };

			GitResult result = await gitCmd.FetchRefsAsync(rfs, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test]
		public async Task TestFetchRefs()
		{
			string[] rfs =
			{
				"+refs/notes/GitMind.Branches:refs/notes/origin/GitMind.Branches",
				"+refs/notes/GitMind.Branches.Manual:refs/notes/origin/GitMind.Branches.Manual"
			};

			GitResult result = await gitCmd.FetchRefsAsync(rfs, ct);
			Assert.IsTrue(result.IsOk);
		}
	}
}