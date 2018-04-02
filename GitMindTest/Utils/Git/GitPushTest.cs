using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitPushTest : GitTestBase<IGitPush>
	{
		[Test, Explicit]
		public async Task Test()
		{
			R result = await gitCmd.PushAsync(ct);
			Assert.IsTrue(result.IsOk);
		}
	}
}