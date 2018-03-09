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
			string version = await gitCmd.GetVersionAsync(ct);
			Assert.AreEqual("2.16.2.windows.1", version);
		}


		[Test, Explicit]
		public async Task TestGitPath()
		{
			string path = await gitCmd.GetGitPathAsync(ct);
			Assert.AreNotEqual("", path);
		}
	}
}