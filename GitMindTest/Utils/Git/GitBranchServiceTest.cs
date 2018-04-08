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
		public async Task TestBranchAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "Text 1");
			await git.CommitAllChangesAsync("Message 1");

			branches = await git.GetBranchesAsync();
			Assert.AreEqual(1, branches.Count);

			await git.BrancheAsync("branch1");
			branches = await git.GetBranchesAsync();
			Assert.AreEqual(2, branches.Count);
			GitBranch2 current = branches.First(branch => branch.IsCurrent);
			Assert.AreEqual("branch1", current.Name);

			Assert.AreEqual(branches[0].TipSha, branches[1].TipSha);
			io.WriteFile("file1.txt", "Text on branch 1");
			await git.CommitAllChangesAsync("Message 1");

			branches = await git.GetBranchesAsync();
			Assert.AreNotEqual(branches[0].TipSha, branches[1].TipSha);

			await git.CheckoutAsync("master");
			branches = await git.GetBranchesAsync();
			current = branches.First(branch => branch.IsCurrent);
			Assert.AreEqual("master", current.Name);
		}


		[Test]
		public async Task TestGetBranchesAsync()
		{
			await git.CloneRepoAsync();

			// Get branches from an empty repo should be 0 branches
			R<IReadOnlyList<GitBranch2>> result = await cmd.GetBranchesAsync(ct);
			Assert.IsTrue(result.IsOk);
			Assert.IsTrue(!result.Value.Any());

			// First commit
			io.WriteFile("file1.txt", "text 1");
			await git.CommitAllChangesAsync("Message 1");

			// After first commit, there is a master branch, which is missing the remote master,
			// since that server repo is still empty untill first push
			result = await cmd.GetBranchesAsync(ct);
			Assert.AreEqual(1, result.Value.Count);
			Assert.AreEqual("master", result.Value[0].Name);
			Assert.AreEqual(true, result.Value[0].IsLocal);
			Assert.AreEqual(true, result.Value[0].IsRemoteMissing);
			Assert.AreEqual(true, result.Value[0].IsPushable);
			Assert.AreEqual(false, result.Value[0].IsFetchable);

			// First push
			await git.PushAsync();

			// After push, we expect 2 branches local and remote
			result = await cmd.GetBranchesAsync(ct);
			Assert.AreEqual(2, result.Value.Count);
			GitBranch2 local = result.Value.First(branch => branch.IsLocal);
			GitBranch2 remote = result.Value.First(branch => branch.IsRemote);
			Assert.AreEqual("master", local.Name);
			Assert.AreEqual(false, local.IsRemoteMissing);
			Assert.AreEqual(false, local.IsPushable);
			Assert.AreEqual(false, local.IsFetchable);
			Assert.AreEqual("origin/master", local.RemoteName);
			Assert.AreEqual("origin/master", remote.Name);
			Assert.AreEqual(false, remote.IsPushable);
			Assert.AreEqual(false, remote.IsFetchable);

			// Second commit
			io.WriteFile("file1.txt", "text 2");
			await git.CommitAllChangesAsync("Message 2");

			// Before second push, the local branch will be ahead 1, but remote is 0 0
			result = await cmd.GetBranchesAsync(ct);
			Assert.AreEqual(2, result.Value.Count);
			local = result.Value.First(branch => branch.IsLocal);
			remote = result.Value.First(branch => branch.IsRemote);
			Assert.AreEqual("master", local.Name);
			Assert.AreEqual(1, local.AheadCount);
			Assert.AreEqual(0, local.BehindCount);
			Assert.AreEqual(true, local.IsPushable);
			Assert.AreEqual(false, local.IsFetchable);
			Assert.AreEqual(true, local.IsPushable);
			Assert.AreEqual("origin/master", remote.Name);
			Assert.AreEqual(0, remote.AheadCount);
			Assert.AreEqual(0, remote.BehindCount);
			Assert.AreEqual(false, remote.IsPushable);
			Assert.AreEqual(false, remote.IsFetchable);

			// Second push
			await git.PushAsync();

			// branch is in synk
			result = await cmd.GetBranchesAsync(ct);
			local = result.Value.First(branch => branch.IsLocal);
			remote = result.Value.First(branch => branch.IsRemote);
			Assert.AreEqual(false, local.IsFetchable);
			Assert.AreEqual(false, local.IsPushable);
			Assert.AreEqual(false, remote.IsFetchable);
			Assert.AreEqual(false, remote.IsPushable);
		}

		[Test]
		public async Task TestBehindAsync()
		{
			await git.CloneRepoAsync();

			// 2 commits
			io.WriteFile("file1.txt", "text 1");
			await git.CommitAllChangesAsync("Message 1");
			io.WriteFile("file1.txt", "text 2");
			await git.CommitAllChangesAsync("Message 2");

			// Push push
			await git.PushAsync();

			await git.UncommitAsync();
			await git.UndoUncommitedAsync();

			branches = await git.GetBranchesAsync();
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

			io.WriteFile("file1.txt", "text 3");
			await git.CommitAllChangesAsync("Message 3");

			branches = await git.GetBranchesAsync();
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
			R<IReadOnlyList<GitBranch2>> result = await cmd.GetBranchesAsync(ct);
		}
	}
}