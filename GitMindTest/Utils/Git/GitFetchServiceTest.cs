using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitFetchServiceTest : GitTestBase<IGitFetchService>
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
			Assert.AreEqual(2, branches.Count);
			Assert.AreEqual(0, branches[0].BehindCount);
			Assert.AreEqual(0, branches[0].AheadCount);

			io.WriteFile("file1.txt", "Text 2");
			await git.CommitAllChangesAsync("Message 2");
			await git.PushAsync();

			R result = await cmd2.FetchAsync(ct);
			Assert.IsTrue(result.IsOk);
			branches = await git2.GetBranchesAsync();
			Assert.AreEqual(1, branches[0].BehindCount);
			Assert.AreEqual(0, branches[0].AheadCount);
		}


		[Test]
		public async Task TestNoRemoteAsync()
		{
			await git.InitRepoAsync();

			R result = await cmd.FetchAsync(ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test]
		public async Task TestFetchBranch()
		{
			await git.CloneRepoAsync();

			io.WriteFile("file1.txt", "Text 1");
			await git.CommitAllChangesAsync("Message 1");
			await git.PushAsync();

			await git.BranchAsync("branch1", true);

			string[] rfs = { "master:master" };

			R result = await cmd.FetchRefsAsync(rfs, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test, Explicit]
		public async Task TestFetchRefs()
		{
			string[] rfs =
			{
				"+refs/notes/GitMind.Branches:refs/notes/origin/GitMind.Branches",
				"+refs/notes/GitMind.Branches.Manual:refs/notes/origin/GitMind.Branches.Manual"
			};

			R result = await cmd.FetchRefsAsync(rfs, ct);
			Assert.IsTrue(result.IsOk);
		}
	}
}