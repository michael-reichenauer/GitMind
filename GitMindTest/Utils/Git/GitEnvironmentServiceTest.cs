using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitEnvironmentServiceTest : GitTestBase<IGitEnvironmentService>
	{
		[Test, Explicit]
		public async Task TestVersion()
		{
			string version = await gitCmd.TryGetGitVersionAsync(ct);
			Assert.AreEqual("2.16.2.windows.1", version);
		}


		[Test, Explicit]
		public async Task TestGitCorePath()
		{
			string path = await gitCmd.TryGetGitCorePathAsync(ct);
			Log.Debug(path);
			Assert.AreNotEqual(null, path);
		}

		[Test, Explicit]
		public async Task TestGitCmdPath()
		{
			string path = await gitCmd.TryGetGitCmdPathAsync(ct);
			Log.Debug(path);
			Assert.AreNotEqual(null, path);
		}


		[Test, Explicit]
		public void TestWorkingFolderRootPath()
		{
			Assert.AreEqual(@"C:\Work Files\GitMind", gitCmd.TryGetWorkingFolderRoot(
				@"C:\Work Files\GitMind\GitMind\Common\MessageDialogs"));

			Assert.AreEqual(@"C:\Work Files\GitMind", gitCmd.TryGetWorkingFolderRoot(
				@"C:\Work Files\GitMind\.git"));


		}
	}
}