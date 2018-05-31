using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Features.Diffing.Private;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitMergeServiceTest : GitTestBase<IGitMergeService>
	{
		[Test]
		public async Task TestMergeAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "Text 1");
			await git.CommitAllChangesAsync("Message 1");

			await git.BranchAsync("branch1");
			io.WriteFile("file1.txt", "Text on branch 1");
			await git.CommitAllChangesAsync("Message 1");

			await git.CheckoutAsync("master");
			Assert.AreEqual("Text 1", io.ReadFile("file1.txt"));

			R result = await cmd.MergeAsync("branch1", ct);
			Assert.AreEqual(true, result.IsOk);

			// Merge has not automatically committed
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.Modified);
			Assert.AreEqual(true, status.IsMerging);
			Assert.AreEqual("Merge branch 'branch1'", status.MergeMessage);

			await git.CommitAllChangesAsync("Message 1");
			status = await git.GetStatusAsync();
			Assert.AreEqual(true, status.OK);
			Assert.AreEqual(false, status.IsMerging);
			Assert.AreEqual("Text on branch 1", io.ReadFile("file1.txt"));
		}

		[Test]
		public async Task TestMergeWithConflictsAsync()
		{
			isCleanUp = false;

			await git.InitRepoAsync();

			// Add some files as initial add on master
			io.WriteFile("file1.txt", "Text 1");
			io.WriteFile("file2.txt", "Text 2");
			io.WriteFile("file3.txt", "Text 3");
			io.WriteFile("file4.txt", "Text 4");
			io.WriteFile("file5.txt", "Text 5");
			await git.CommitAllChangesAsync("Initial add on master");

			// Create branch1
			await git.BranchAsync("branch1");
			io.WriteFile("file1.txt", "Text 12 on branch\r\n\n");
			io.DeleteFile("file2.txt");                           // Deleted 2 on branch
			io.WriteFile("file3.txt", "Text 32 on branch");
			io.WriteFile("file4.txt", "Text 42 on branch");
			io.DeleteFile("file5.txt");                           // Delete 5 on branch
			io.WriteFile("file6.txt", "Text 62 on branch");        // Added 6 on branch
			await git.CommitAllChangesAsync("Message 1 on branch1");

			// Switch to master and make some changes and commit
			await git.CheckoutAsync("master");
			io.WriteFile("file1.txt", "Text 12 on master\n");
			io.WriteFile("file2.txt", "Text 22 on master");
			io.DeleteFile("file3.txt");                           // Delete 3 on master
																														// No change on file 4
			io.DeleteFile("file5.txt");                           // Delete 5 om master
			io.WriteFile("file6.txt", "Text 62 on master");        // added on master
			await git.CommitAllChangesAsync("Message 2 on master");

			// Merge branch to master, expecting 1CMM, 2CMD, 3CDM, 4M, (no 5), 6CAA
			R result = await cmd.MergeAsync("branch1", ct);
			Assert.AreEqual(true, result.IsOk);
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.Modified);
			Assert.AreEqual(4, status.Conflicted);
			Assert.AreEqual(true, status.IsMerging);

			GitConflicts conflicts = await git.GetConflictsAsync();
			Assert.AreEqual(true, conflicts.HasConflicts);
			Assert.AreEqual(4, conflicts.Count);

			Assert.AreEqual(true, conflicts.Files[0].Status.HasFlag(GitFileStatus.ConflictMM));
			Assert.AreEqual("Text 1", await git.GetConflictFileAsync(conflicts.Files[0].BaseId));
			Assert.AreEqual("Text 12 on master\n", await git.GetConflictFileAsync(conflicts.Files[0].LocalId));
			Assert.AreEqual("Text 12 on branch\n\n", await git.GetConflictFileAsync(conflicts.Files[0].RemoteId));

			Assert.AreEqual(true, conflicts.Files[1].Status.HasFlag(GitFileStatus.ConflictMD));
			Assert.AreEqual("Text 2", await git.GetConflictFileAsync(conflicts.Files[1].BaseId));
			Assert.AreEqual("Text 22 on master", await git.GetConflictFileAsync(conflicts.Files[1].LocalId));
			Assert.AreEqual(null, conflicts.Files[1].RemoteId);

			Assert.AreEqual(true, conflicts.Files[2].Status.HasFlag(GitFileStatus.ConflictDM));
			Assert.AreEqual("Text 3", await git.GetConflictFileAsync(conflicts.Files[2].BaseId));
			Assert.AreEqual(null, conflicts.Files[2].LocalId);
			Assert.AreEqual("Text 32 on branch", await git.GetConflictFileAsync(conflicts.Files[2].RemoteId));

			Assert.AreEqual(true, conflicts.Files[3].Status.HasFlag(GitFileStatus.ConflictAA));
			Assert.AreEqual(null, conflicts.Files[3].BaseId);
			Assert.AreEqual("Text 62 on master", await git.GetConflictFileAsync(conflicts.Files[3].LocalId));
			Assert.AreEqual("Text 62 on branch", await git.GetConflictFileAsync(conflicts.Files[3].RemoteId));
		}

		[Test]
		public async Task TestConflictsResolveAsync()
		{
			GitDiffParser diffParser = new GitDiffParser();

			await git.InitRepoAsync();

			// Add some files as initial add on master
			io.WriteFile("file1.txt", "Text 1");
			io.WriteFile("file2.txt", "Text 2");
			io.WriteFile("file3.txt", "Text 3");
			io.WriteFile("file4.txt", "Text 4");
			io.WriteFile("file5.txt", "Text 5");
			GitCommit masterCommit1 = await git.CommitAllChangesAsync("Initial add on master");

			// Create branch1
			await git.BranchAsync("branch1");
			io.WriteFile("file1.txt", "Text 12 on branch\r\n\n");
			io.DeleteFile("file2.txt");                           // Deleted 2 on branch
			io.WriteFile("file3.txt", "Text 32 on branch");
			io.WriteFile("file4.txt", "Text 42 on branch");
			io.DeleteFile("file5.txt");                           // Delete 5 on branch
			io.WriteFile("file6.txt", "Text 62 on branch");        // Added 6 on branch
			GitCommit branchCommit1 = await git.CommitAllChangesAsync("Message 1 on branch1");

			// Switch to master and make some changes and commit
			await git.CheckoutAsync("master");
			io.WriteFile("file1.txt", "Text 12 on master\n");
			io.WriteFile("file2.txt", "Text 22 on master");
			io.DeleteFile("file3.txt");                           // Delete 3 on master
																														// No change on file 4
			io.DeleteFile("file5.txt");                           // Delete 5 om master
			io.WriteFile("file6.txt", "Text 62 on master");        // added on master
			GitCommit masterCommit2 = await git.CommitAllChangesAsync("Message 2 on master");

			// Merge branch to master, expecting 1CMM, 2CMD, 3CDM, 4M, (no 5), 6CAA
			R result = await cmd.MergeAsync("branch1", ct);

			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.Modified);
			Assert.AreEqual(4, status.Conflicted);
			Assert.AreEqual(true, status.IsMerging);

			GitConflicts conflicts = await git.GetConflictsAsync();
			Assert.AreEqual(true, conflicts.HasConflicts);
			Assert.AreEqual(4, conflicts.Count);


			io.WriteFile("file1.txt", "Text 13 merged");
			status = await git.GetStatusAsync();
			await git.Service<IGitStatusService>().Call(m => m.AddAsync("file1.txt", ct));
			status = await git.GetStatusAsync();
			Assert.AreEqual(2, status.Modified);
			Assert.AreEqual(3, status.Conflicted);

			io.DeleteFile("file2.txt");
			await git.Service<IGitStatusService>().Call(m => m.RemoveAsync("file2.txt", ct));
			status = await git.GetStatusAsync();
			Assert.AreEqual(2, status.Modified);
			Assert.AreEqual(1, status.Deleted);
			Assert.AreEqual(2, status.Conflicted);


			string branchSide = await git.GetConflictFileAsync(conflicts.Files[2].RemoteId);
			io.WriteFile("file3.txt", branchSide);
			await git.Service<IGitStatusService>().Call(m => m.AddAsync("file3.txt", ct));
			status = await git.GetStatusAsync();
			Assert.AreEqual(3, status.Modified);
			Assert.AreEqual(1, status.Deleted);
			Assert.AreEqual(1, status.Conflicted);

			io.WriteFile("file6.txt", "Text 63 merged");
			await git.Service<IGitStatusService>().Call(m => m.AddAsync("file6.txt", ct));
			status = await git.GetStatusAsync();
			Assert.AreEqual(4, status.Modified);
			Assert.AreEqual(1, status.Deleted);
			Assert.AreEqual(0, status.Conflicted);


			GitCommit mergeCommit = await git.CommitAllChangesAsync(status.MergeMessage);
			string mergePatch = await git.Service<IGitDiffService>().Call(m => m.GetCommitDiffAsync(mergeCommit.Sha.Sha, ct));
			string mergePatch2 = await git.Service<IGitDiffService>().Call(
				m => m.GetCommitDiffAsync(mergeCommit.Sha.Sha, ct));
			CommitDiff diff = await diffParser.ParseAsync(mergeCommit.Sha, mergePatch, true, false);
			CommitDiff diff2 = await diffParser.ParseAsync(mergeCommit.Sha, mergePatch2, true, false);
			string left = File.ReadAllText(diff.LeftPath);
			string right = File.ReadAllText(diff.RightPath);

			string left2 = File.ReadAllText(diff2.LeftPath);
			string right2 = File.ReadAllText(diff2.RightPath);
		}


		[Test]
		public async Task TestMergeCommitAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "Text 1");
			await git.CommitAllChangesAsync("Message 1 on master");

			await git.BranchAsync("branch1");
			io.WriteFile("file1.txt", "Text on branch 1");
			GitCommit commit = await git.CommitAllChangesAsync("Message 1on branch1");

			io.WriteFile("file1.txt", "Text on branch 2");
			await git.CommitAllChangesAsync("Message 2on branch 1");

			await git.CheckoutAsync("master");
			Assert.AreEqual("Text 1", io.ReadFile("file1.txt"));

			R result = await cmd.MergeAsync(commit.Sha.Sha, ct);
			Assert.AreEqual(true, result.IsOk);

			// Merge has not automatically committed
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.Modified);
			Assert.AreEqual(true, status.IsMerging);
			Assert.AreEqual($"Merge commit '{commit.Sha.Sha}'", status.MergeMessage);

			await git.CommitAllChangesAsync("Message 2 on master");
			status = await git.GetStatusAsync();
			Assert.AreEqual(true, status.OK);
			Assert.AreEqual(false, status.IsMerging);
			Assert.AreEqual("Text on branch 1", io.ReadFile("file1.txt"));

			log = await git.GetLogAsync();
			log.ForEach(c => Log.Debug($"{c}"));
		}

		[Test]
		public async Task TestMergeFastForwardAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "Text 1");
			await git.CommitAllChangesAsync("Message 1");

			await git.BranchAsync("branch1");
			io.WriteFile("file1.txt", "Text on branch 1");

			await git.CommitAllChangesAsync("Message 1 on branch1");

			await git.CheckoutAsync("master");


			R<bool> result = await cmd.TryMergeFastForwardAsync("branch1", ct);
			Assert.AreEqual(true, result.IsOk);
			Assert.AreEqual(true, result.Value);

			log = await git.GetLogAsync();
			Assert.AreEqual(2, log.Count);
			Assert.AreEqual(1, log[0].ParentIds.Count);
			Assert.AreEqual(log[1].Sha.ShortSha, log[0].ParentIds[0].AsText);
			Assert.AreEqual("Message 1 on branch1", log[0].Subject);
			Assert.AreEqual("Text on branch 1", io.ReadFile("file1.txt"));
		}

		[Test]
		public async Task TestMergeFastForwardFailedAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "Text 1 on master");
			await git.CommitAllChangesAsync("Message 1 on master");

			await git.BranchAsync("branch1");
			io.WriteFile("file1.txt", "Text 1 on branch 1");

			await git.CommitAllChangesAsync("Message 1 on branch1");

			await git.CheckoutAsync("master");
			io.WriteFile("file1.txt", "Text 2 on master");
			await git.CommitAllChangesAsync("Message 1 on master");

			R<bool> result = await cmd.TryMergeFastForwardAsync("branch1", ct);
			Assert.AreEqual(true, result.IsOk);
			Assert.AreEqual(false, result.Value);

			await git.MergeAsync("branch1");
		}
	}
}