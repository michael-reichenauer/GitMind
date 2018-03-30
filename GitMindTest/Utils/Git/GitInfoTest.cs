using System.Threading.Tasks;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitInfoTest : GitTestBase<IGitInfo>
	{
		[Test, Explicit]
		public async Task TestVersion()
		{
			string version = await gitCmd.TryGetGitVersionAsync(ct);
			Assert.AreEqual("2.16.2.windows.1", version);
		}


		[Test, Explicit]
		public async Task TestGitPath()
		{
			string path = await gitCmd.TryGetGitPathAsync(ct);
			Assert.AreNotEqual(null, path);
		}


		[Test, Explicit]
		public async Task TestWorkingFolderRootPath()
		{
			Assert.AreEqual(@"C:\Work Files\GitMind", await gitCmd.TryGetWorkingFolderRootAsync(
				@"C:\Work Files\GitMind\GitMind\Common\MessageDialogs", ct));

			Assert.AreEqual(@"C:\Work Files\GitMind", await gitCmd.TryGetWorkingFolderRootAsync(
				@"C:\Work Files\GitMind\.git", ct));


		}
	}
}