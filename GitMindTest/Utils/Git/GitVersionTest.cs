using System.Threading.Tasks;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitVersionTest : GitTestBase<IGitVersion>
	{
		[Test, Explicit]
		public async Task Test()
		{
			string version = await gitCmd.GetAsync(ct);
			Assert.AreEqual("git version 2.16.2.windows.1\r\n", version);
		}
	}
}