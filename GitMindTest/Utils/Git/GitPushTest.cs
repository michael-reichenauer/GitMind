using System.Threading.Tasks;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitPushTest : GitTestBase<IGitPushService>
	{
		[Test]
		public async Task TestPusAsync()
		{
			await CloneRepoAsync();

			FileWrite("file1.txt", "text 1");
			await CommitAllChangesAsync("Message 1");

			IGitBranchService2 branchService2 = am.Resolve<IGitBranchService2>();
			var aheadBehind = await branchService2.GetAheadBehindAsync("master", ct);

			//R result = await gitCmd.PushAsync(ct);
			//Assert.IsTrue(result.IsOk);

			//status = await GetStatusAsync();

			//FileWrite("file1.txt", "text 2");
			//await CommitAllChangesAsync("Message 2");

			//status = await GetStatusAsync();
		}
	}
}