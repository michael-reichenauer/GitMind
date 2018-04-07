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
		public async Task TestPushAsync()
		{
			await CloneRepoAsync();

			FileWrite("file1.txt", "text 1");
			await CommitAllChangesAsync("Message 1");

			var branches = await GetBranchesAsync();

			//R result = await gitCmd.PushAsync(ct);
			//Assert.IsTrue(result.IsOk);

			//status = await GetStatusAsync();

			//FileWrite("file1.txt", "text 2");
			//await CommitAllChangesAsync("Message 2");

			//status = await GetStatusAsync();
		}
	}
}