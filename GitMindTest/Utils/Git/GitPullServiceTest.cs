using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitPullServiceTest : GitTestBase<IGitPullService>
	{
		[Test]
		public async Task TestFetch()
		{
			await git.CloneRepoAsync();

			io.WriteFile("file1.txt", "Text 1");
			await git.CommitAllChangesAsync("Message 1");
			await git.PushAsync();

			await git2.CloneRepoAsync(git.OriginUri);
			branches = await git2.GetBranchesAsync();
			Assert.AreEqual(0, branches[0].BehindCount);
			Assert.AreEqual(0, branches[0].AheadCount);

			io.WriteFile("file1.txt", "Text 2");
			await git.CommitAllChangesAsync("Message 2");
			await git.PushAsync();

			await git2.FetchAsync();
			branches = await git2.GetBranchesAsync();
			Assert.AreEqual(1, branches[0].BehindCount);
			Assert.AreEqual(0, branches[0].AheadCount);

			R result = await cmd2.PullAsync(ct);
			Assert.AreEqual(true, result.IsOk);
			branches = await git2.GetBranchesAsync();
			Assert.AreEqual(0, branches[0].BehindCount);
			Assert.AreEqual(0, branches[0].AheadCount);
		}
	}
}