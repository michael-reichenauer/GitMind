using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitCommitServiceTest : GitTestBase<IGitCommitService2>
	{
		[Test]
		public async Task TestCommit()
		{
			await git.InitRepoAsync();

			// Write a file and check status
			io.WriteFile("file1.txt", "some text");
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);

			// Commit and then check status
			R<GitCommit> result = await cmd.CommitAllChangesAsync("Some message 1", ct);
			Assert.IsTrue(result.IsOk);
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);

			// Get commit files
			var files = await cmd.GetCommitFilesAsync(result.Value.Sha.Sha, ct);
			Assert.AreEqual(1, files.Value.Count);
			Assert.IsNotNull(files.Value.FirstOrDefault(f => f.FilePath == "file1.txt"));

			// Make one more commit and check status
			io.WriteFile("file2.txt", "some text");
			result = await cmd.CommitAllChangesAsync("Some message 2", ct);
			Assert.IsTrue(result.IsOk);
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
		}


		[Test]
		public async Task TestCommitAllChangesAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "some text");

			R<GitCommit> result = await git.CommitAllChangesAsync("Some message 1");
			Assert.IsTrue(result.IsOk);
		}


		[Test]
		public async Task TestUndoCommitAsync()
		{
			await git.InitRepoAsync();

			// Make 2 commits on a file
			io.WriteFile("file1.txt", "Some text 1");
			GitCommit commit1 = await git.CommitAllChangesAsync("Message 1");
			io.WriteFile("file1.txt", "Some text 2");
			GitCommit commit2 = await git.CommitAllChangesAsync("Message 2");

			// Undo the last commit and check that the previous version of the file exists but is not commited
			R result = await cmd.UndoCommitAsync(commit2.Sha.Sha, ct);
			Assert.IsTrue(result.IsOk);
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);
			Assert.AreEqual(1, status.Modified);
			Assert.AreEqual("Some text 1", io.ReadFile("file1.txt"));

			// Clean the modifications and check that the last version of the file exists
			await git.CleanWorkingFolderAsync();
			Assert.AreEqual("Some text 2", io.ReadFile("file1.txt"));

			// Trying to undo first commit will cause a conflict since there is a file in the work folder
			result = await cmd.UndoCommitAsync(commit1.Sha.Sha, ct);
			Assert.IsTrue(result.IsFaulted);
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);
			Assert.AreEqual(1, status.Conflicted);
			Assert.IsTrue(io.ExistsFile("file1.txt"));

			// Clean the modifications and check that the last version of the file exists
			await git.CleanWorkingFolderAsync();
			Assert.AreEqual("Some text 2", io.ReadFile("file1.txt"));
		}


		[Test]
		public async Task TestUncommitAsync()
		{
			await git.InitRepoAsync();

			// Make 2 commits on a file
			io.WriteFile("file1.txt", "Some text 1");
			GitCommit commit1 = await git.CommitAllChangesAsync("Message 1");
			io.WriteFile("file1.txt", "Some text 2");
			GitCommit commit2 = await git.CommitAllChangesAsync("Message 2");

			status = await git.GetStatusAsync();
			Assert.AreEqual(true, status.OK);

			branches = await git.GetBranchesAsync();
			Assert.AreEqual(commit2.Sha, branches[0].TipSha);

			R<GitCommit> commit = await cmd.GetCommitAsync(commit2.Sha.Sha, ct);
			Assert.AreEqual(true, commit.IsOk);

			R result = await cmd.UnCommitAsync(ct);
			Assert.AreEqual(true, result.IsOk);

			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.Modified);

			branches = await git.GetBranchesAsync();
			Assert.AreEqual(commit1.Sha, branches[0].TipSha);

			await git.UndoUncommitedAsync();
			status = await git.GetStatusAsync();
			Assert.AreEqual(true, status.OK);
		}


		[Test]
		public async Task TestUncommitMergedIntoMasterCommitAsync()
		{
			await git.InitRepoAsync();

			// Make 2 commits on a file
			io.WriteFile("file1.txt", "Some text 1");
			GitCommit commit1 = await git.CommitAllChangesAsync("Message 1");
			io.WriteFile("file1.txt", "Some text 2");
			GitCommit commit2 = await git.CommitAllChangesAsync("Message 2");

			await git.BrancheAsync("branch1");

			io.WriteFile("file1.txt", "Some text on branch 1");
			GitCommit commit3 = await git.CommitAllChangesAsync("Message  on branch 1");

			await git.CheckoutAsync("master");
			await git.MergeAsync("branch1");
			status = await git.GetStatusAsync();
			await git.CommitAllChangesAsync($"{status.MergeMessage} into master");

			R result = await cmd.UnCommitAsync(ct);
			Assert.AreEqual(true, result.IsOk);
		}


		[Test]
		public async Task TestUncommitMergedIntoBranchCommitAsync()
		{
			await git.InitRepoAsync();

			// Make 2 commits on a file
			io.WriteFile("file1.txt", "Some text 1");
			GitCommit commit1 = await git.CommitAllChangesAsync("Message 1");
			io.WriteFile("file1.txt", "Some text 2");
			GitCommit commit2 = await git.CommitAllChangesAsync("Message 2");

			await git.BrancheAsync("branch1");

			io.WriteFile("file1.txt", "Some text on branch 1");
			GitCommit commit3 = await git.CommitAllChangesAsync("Message  on branch 1");

			await git.CheckoutAsync("master");
			io.WriteFile("file2.txt", "Some text 3");
			GitCommit commit4 = await git.CommitAllChangesAsync("Message 3 on master");

			await git.CheckoutAsync("branch1");

			await git.MergeAsync("master");
			status = await git.GetStatusAsync();
			await git.CommitAllChangesAsync($"{status.MergeMessage}");

			R result = await cmd.UnCommitAsync(ct);
			Assert.AreEqual(true, result.IsOk);
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.Added);
			Assert.AreEqual("file2.txt", status.Files[0].FilePath);
		}


		[Test]
		public async Task TestCommitingAsync()
		{
			// Init default working folder repo
			await git.InitRepoAsync();

			// Write file and check status that a file has been added
			io.WriteFile("file1.txt", "Some text 1");
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.Added);

			// Commit and check status that staus  has no changes
			GitCommit commit1 = await git.CommitAllChangesAsync("Message 1");
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);

			// Make a change to to file and check that status is 1 file modified
			io.WriteFile("file1.txt", "Some text 2");
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.Modified);

			// Commit and verify status
			GitCommit commit2 = await git.CommitAllChangesAsync("Message 2");
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
		}
	}
}