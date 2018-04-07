using System.Threading.Tasks;
using GitMind.Utils;
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

			// After cloning an empty repo, there are no branches until first commit
			branches = await GetBranchesAsync();
			Assert.AreEqual(0, branches.Count);

			// First commit
			FileWrite("file1.txt", "text 1");
			await CommitAllChangesAsync("Message 1");

			// After first commit, but before first push, only the local master exist,
			// The remote master has not yet been created and is thus "missing"
			branches = await GetBranchesAsync();
			Assert.AreEqual(1, branches.Count);
			Assert.AreEqual(true, branches[0].IsRemoteMissing);

			// First push
			R result = await gitCmd.PushAsync(ct);
			Assert.AreEqual(true, result.IsOk);

			// Now there are two branches one local and one remote
			branches = await GetBranchesAsync();
			Assert.AreEqual(2, branches.Count);
			Assert.AreEqual(false, branches[0].IsRemoteMissing);

			// Second commit
			FileWrite("file1.txt", "text 2");
			await CommitAllChangesAsync("Message 2");

			// Now local branch is 1 ahead
			branches = await GetBranchesAsync();
			Assert.AreEqual(1, branches[0].AheadCount);

			// Second push
			result = await gitCmd.PushAsync(ct);
			Assert.AreEqual(true, result.IsOk);

			// Branch is insync
			branches = await GetBranchesAsync();
			Assert.AreEqual(0, branches[0].AheadCount);
		}
	}
}