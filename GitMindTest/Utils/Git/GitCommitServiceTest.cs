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
			await InitRepoAsync();

			// Write a file and check status
			WriteFile("file1.txt", "some text");
			Status2 status = await GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);

			// Commit and then check status
			R<GitCommit> result = await gitCmd.CommitAllChangesAsync("Some message 1", ct);
			Assert.IsTrue(result.IsOk);
			status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);

			// Get commit files
			var files = await gitCmd.GetCommitFilesAsync(result.Value.Sha.Sha, ct);
			Assert.AreEqual(1, files.Value.Count);
			Assert.IsNotNull(files.Value.FirstOrDefault(f => f.FilePath == "file1.txt"));

			// Make one more commit and check status
			WriteFile("file2.txt", "some text");
			result = await gitCmd.CommitAllChangesAsync("Some message 2", ct);
			Assert.IsTrue(result.IsOk);
			status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
		}


		[Test]
		public async Task TestCommitAllChangesAsync()
		{
			await InitRepoAsync();

			WriteFile("file1.txt", "some text");

			R<GitCommit> result = await CommitAllChangesAsync("Some message 1");
			Assert.IsTrue(result.IsOk);
		}


		[Test]
		public async Task TestUndoUncommitedAsync()
		{
			await InitRepoAsync();

			// Add a new file, check status that the file is considdered added
			WriteFile("file1.txt", "some text");
			Status2 status = await GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);
			Assert.AreEqual(1, status.Added);

			// Undo all chnages and check status
			R result = await gitCmd.UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);
			status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);

			// Add a new file and commit and check status
			WriteFile("file1.txt", "some text");
			await CommitAllChangesAsync("message 1");
			status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);

			// Edit the tracked file and check status that it is modified (not added)
			WriteFile("file1.txt", "some text 2");
			status = await GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);
			Assert.AreEqual(1, status.Modified);

			// Undo all changes and check status
			result = await gitCmd.UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);
			status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
		}

		[Test]
		public async Task TestUndoUncommitedLockedFileAsync()
		{
			await InitRepoAsync();

			// Add a new file and open file to ensure it is locked
			WriteFile("file1.txt", "some text");
			WriteFile("file2.txt", "some text");
			WriteFile("file3.txt", "some text");
			WriteFile("file4.txt", "some text");
			WriteFile("file5.txt", "some text");
			WriteFile("file6.txt", "some text");

			// Make sure some files are locked
			using (FileStream fileStream2 = File.OpenWrite(GetPath("file2.txt")))
			using (FileStream fileStream3 = File.OpenWrite(GetPath("file3.txt")))
			using (FileStream fileStream5 = File.OpenWrite(GetPath("file5.txt")))
			{
				// Trying to clean folder will not remove locked files, but will return list of them
				R<IReadOnlyList<string>> result = await gitCmd.UndoUncommitedAsync(ct);
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
			await InitRepoAsync();

			// Writing a .sup file, which usually is ignored, but since not yet a .ignore file
			// The staus will show the file and it does exist
			WriteFile("file1.suo", "some text");
			Status2 status = await GetStatusAsync();
			Assert.AreEqual(1, status.AllChanges);

			// Using the UndoUncommitedAsync() will succeede, and it will remove the file
			R<IReadOnlyList<string>> result = await gitCmd.UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);
			Assert.AreEqual(0, result.Value.Count);
			status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
			Assert.IsFalse(File.Exists(GetPath("file1.suo")));

			// Adding a .gitignoire file to ignore .suo files
			WriteFile(".gitignore", "*.suo\n");
			await CommitAllChangesAsync("Added .gitignore file");

			// Writing a .suo file and make sure staus ignores it but the file does exists
			WriteFile("file1.suo", "some text");
			status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
			Assert.IsTrue(File.Exists(GetPath("file1.suo")));

			// Using the UndoUncommitedAsync() will succeede, but since  file ignored will not remove the file
			result = await gitCmd.UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);
			Assert.AreEqual(0, result.Value.Count);
			status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
			Assert.IsTrue(File.Exists(GetPath("file1.suo")));

			// But using CleanWorkingFolderAsync will remove the file
			result = await gitCmd.CleanWorkingFolderAsync(ct);
			Assert.IsTrue(result.IsOk);
			Assert.AreEqual(0, result.Value.Count);
			status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
			Assert.IsFalse(File.Exists(GetPath("file1.suo")));
		}
	}
}