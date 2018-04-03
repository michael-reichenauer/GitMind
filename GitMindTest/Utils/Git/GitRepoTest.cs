using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitRepoTest : GitTestBase<IGitRepo>
	{
		[Test, Explicit]
		public async Task TestInitRepo()
		{
			string path = CreateTmpDir();

			R result = await gitCmd.InitAsync(path, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test, Explicit]
		public async Task TestCreateNewRepo()
		{
			string path = await GetNewGitRepoAsync();
			Assert.IsNotNull(path);
		}
	}
}