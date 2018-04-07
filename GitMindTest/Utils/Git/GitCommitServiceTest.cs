﻿using System.Collections.Generic;
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
		public async Task TestUndoUncommitedAsync()
		{
			await git.InitRepoAsync();

			// Add a new file, check status that the file is considdered added
			io.WriteFile("file1.txt", "some text");
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);
			Assert.AreEqual(1, status.Added);

			// Undo all chnages and check status
			R result = await cmd.UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);

			// Add a new file and commit and check status
			io.WriteFile("file1.txt", "some text");
			await git.CommitAllChangesAsync("message 1");
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);

			// Edit the tracked file and check status that it is modified (not added)
			io.WriteFile("file1.txt", "some text 2");
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);
			Assert.AreEqual(1, status.Modified);

			// Undo all changes and check status
			result = await cmd.UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
		}

		[Test]
		public async Task TestUndoUncommitedLockedFileAsync()
		{
			await git.InitRepoAsync();

			// Add a new file and open file to ensure it is locked
			io.WriteFile("file1.txt", "some text");
			io.WriteFile("file2.txt", "some text");
			io.WriteFile("file3.txt", "some text");
			io.WriteFile("file4.txt", "some text");
			io.WriteFile("file5.txt", "some text");
			io.WriteFile("file6.txt", "some text");

			// Make sure some files are locked
			using (FileStream fileStream2 = File.OpenWrite(io.FullPath("file2.txt")))
			using (FileStream fileStream3 = File.OpenWrite(io.FullPath("file3.txt")))
			using (FileStream fileStream5 = File.OpenWrite(io.FullPath("file5.txt")))
			{
				// Trying to clean folder will not remove locked files, but will return list of them
				R<IReadOnlyList<string>> result = await cmd.UndoUncommitedAsync(ct);
				Assert.IsTrue(result.IsOk);
				Assert.AreEqual(3, result.Value.Count);
				Assert.That(result.Value, Contains.Item("file2.txt"));
				Assert.That(result.Value, Contains.Item("file3.txt"));
				Assert.That(result.Value, Contains.Item("file5.txt"));
			}
		}


		[Test]
		public async Task TestCleanFolerAsync()
		{
			await git.InitRepoAsync();

			// Writing a .sup file, which usually is ignored, but since not yet a .ignore file
			// The staus will show the file and it does exist
			io.WriteFile("file1.suo", "some text");
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);

			// Using the UndoUncommitedAsync() will succeede, and it will remove the file
			R<IReadOnlyList<string>> result = await cmd.UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);
			Assert.AreEqual(0, result.Value.Count);
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
			Assert.IsFalse(io.ExistsFile("file1.suo"));

			// Adding a .gitignoire file to ignore .suo files
			io.WriteFile(".gitignore", "*.suo\n");
			await git.CommitAllChangesAsync("Added .gitignore file");

			// Writing a .suo file and make sure staus ignores it but the file does exists
			io.WriteFile("file1.suo", "some text");
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
			Assert.IsTrue(io.ExistsFile("file1.suo"));

			// Using the UndoUncommitedAsync() will succeede, but since  file ignored will not remove the file
			result = await cmd.UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);
			Assert.AreEqual(0, result.Value.Count);
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
			Assert.IsTrue(io.ExistsFile("file1.suo"));

			// But using CleanWorkingFolderAsync will remove the file
			result = await cmd.CleanWorkingFolderAsync(ct);
			Assert.IsTrue(result.IsOk);
			Assert.AreEqual(0, result.Value.Count);
			status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
			Assert.IsFalse(io.ExistsFile("file1.suo"));
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
			await cmd.CleanWorkingFolderAsync(ct);
			Assert.AreEqual("Some text 2", io.ReadFile("file1.txt"));

			// Trying to undo first commit will cause a conflict since there is a file in the work folder
			result = await cmd.UndoCommitAsync(commit1.Sha.Sha, ct);
			Assert.IsTrue(result.IsFaulted);
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);
			Assert.AreEqual(1, status.Conflicted);
			Assert.IsTrue(io.ExistsFile("file1.txt"));

			// Clean the modifications and check that the last version of the file exists
			await cmd.CleanWorkingFolderAsync(ct);
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

			R undo = await cmd.UndoUncommitedAsync(ct);
			Assert.AreEqual(true, undo.IsOk);
			status = await git.GetStatusAsync();
			Assert.AreEqual(true, status.OK);
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