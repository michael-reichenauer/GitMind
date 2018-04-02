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
		public void TestVersion()
		{
			string version = gitCmd.TryGetGitVersion();
			Assert.AreEqual("2.16.2.windows.1", version);
		}


		[Test, Explicit]
		public void TestGitCorePath()
		{
			string path = gitCmd.TryGetGitCorePath();
			Log.Debug(path);
			Assert.AreNotEqual(null, path);
		}

		[Test, Explicit]
		public void TestGitCmdPath()
		{
			string path = gitCmd.TryGetGitCmdPath();
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