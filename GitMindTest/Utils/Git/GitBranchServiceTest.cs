using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitBranchServiceTest : GitTestBase<IGitBranchService2>
	{
		[Test]
		public async Task TestPusAsync()
		{
			await CloneRepoAsync();

			// Get branches from an empty repo should be 0 branches
			R<IReadOnlyList<GitBranch2>> branches = await gitCmd.GetBranchesAsync(ct);
			Assert.IsTrue(branches.IsOk);
			Assert.IsTrue(!branches.Value.Any());

			// First commit
			FileWrite("file1.txt", "text 1");
			await CommitAllChangesAsync("Message 1");

			// After first commit, there is a master branch, which is missing the remote master,
			// since that server repo is still empty untill first push
			branches = await gitCmd.GetBranchesAsync(ct);
			Assert.AreEqual(1, branches.Value.Count);
			Assert.AreEqual("master", branches.Value[0].BranchName);
			Assert.AreEqual(true, branches.Value[0].IsLocal);
			Assert.AreEqual(true, branches.Value[0].IsRemoteMissing);
			Assert.AreEqual(true, branches.Value[0].IsPushable);
			Assert.AreEqual(false, branches.Value[0].IsFetchable);

			// First push
			await PushAsync();

			// After push, we expect 2 branches local and remote
			branches = await gitCmd.GetBranchesAsync(ct);
			Assert.AreEqual(2, branches.Value.Count);
			GitBranch2 local = branches.Value.First(branch => branch.IsLocal);
			GitBranch2 remote = branches.Value.First(branch => branch.IsRemote);
			Assert.AreEqual("master", local.BranchName);
			Assert.AreEqual(false, local.IsRemoteMissing);
			Assert.AreEqual(false, local.IsPushable);
			Assert.AreEqual(false, local.IsFetchable);
			Assert.AreEqual("origin/master", local.BoundBranchName);
			Assert.AreEqual("origin/master", remote.BranchName);
			Assert.AreEqual(false, remote.IsPushable);
			Assert.AreEqual(false, remote.IsFetchable);

			// Second commit
			FileWrite("file1.txt", "text 2");
			await CommitAllChangesAsync("Message 2");

			// Before second push, the local branch will be ahead 1, but remote is 0 0
			branches = await gitCmd.GetBranchesAsync(ct);
			Assert.AreEqual(2, branches.Value.Count);
			local = branches.Value.First(branch => branch.IsLocal);
			remote = branches.Value.First(branch => branch.IsRemote);
			Assert.AreEqual("master", local.BranchName);
			Assert.AreEqual(1, local.AheadCount);
			Assert.AreEqual(0, local.BehindCount);
			Assert.AreEqual(true, local.IsPushable);
			Assert.AreEqual(false, local.IsFetchable);
			Assert.AreEqual(true, local.IsPushable);
			Assert.AreEqual("origin/master", remote.BranchName);
			Assert.AreEqual(0, remote.AheadCount);
			Assert.AreEqual(0, remote.BehindCount);
			Assert.AreEqual(false, remote.IsPushable);
			Assert.AreEqual(false, remote.IsFetchable);

			// Second push
			await PushAsync();

			// branch is in synk
			branches = await gitCmd.GetBranchesAsync(ct);
			local = branches.Value.First(branch => branch.IsLocal);
			remote = branches.Value.First(branch => branch.IsRemote);
			Assert.AreEqual(false, local.IsFetchable);
			Assert.AreEqual(false, local.IsPushable);
			Assert.AreEqual(false, remote.IsFetchable);
			Assert.AreEqual(false, remote.IsPushable);
		}

		[Test]
		public async Task TestBehindAsync()
		{
			await CloneRepoAsync();

			// 2 commits
			FileWrite("file1.txt", "text 1");
			await CommitAllChangesAsync("Message 1");
			FileWrite("file1.txt", "text 2");
			await CommitAllChangesAsync("Message 2");

			// Push push
			await PushAsync();

			await UncommitAsync();
			await UndoUncommitedAsync();

			branches = await GetBranchesAsync();
			GitBranch2 local = branches.First(branch => branch.IsLocal);
			GitBranch2 remote = branches.First(branch => branch.IsRemote);
			Assert.AreEqual(0, local.AheadCount);
			Assert.AreEqual(1, local.BehindCount);
			Assert.AreEqual(true, local.IsFetchable);
			Assert.AreEqual(false, local.IsPushable);
			Assert.AreEqual(0, remote.AheadCount);
			Assert.AreEqual(0, remote.BehindCount);
			Assert.AreEqual(false, remote.IsFetchable);
			Assert.AreEqual(false, remote.IsPushable);

			FileWrite("file1.txt", "text 3");
			await CommitAllChangesAsync("Message 3");

			branches = await GetBranchesAsync();
			local = branches.First(branch => branch.IsLocal);
			remote = branches.First(branch => branch.IsRemote);
			Assert.AreEqual(1, local.AheadCount);
			Assert.AreEqual(1, local.BehindCount);
			Assert.AreEqual(false, local.IsFetchable);
			Assert.AreEqual(false, local.IsPushable);
			Assert.AreEqual(0, remote.AheadCount);
			Assert.AreEqual(0, remote.BehindCount);
			Assert.AreEqual(false, remote.IsFetchable);
			Assert.AreEqual(false, remote.IsPushable);
		}


		[Test, Explicit]
		public async Task Test()
		{
			R<IReadOnlyList<GitBranch2>> branches = await gitCmd.GetBranchesAsync(ct);
		}
	}
}