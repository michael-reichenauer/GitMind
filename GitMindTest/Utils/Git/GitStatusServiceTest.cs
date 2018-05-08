using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitStatusServiceTest : GitTestBase<IGitStatusService>
	{
		[Test]
		public async Task TestStatus()
		{
			await git.InitRepoAsync();

			R<GitStatus2> result = await cmd.GetStatusAsync(ct);
			Assert.IsTrue(result.IsOk);
			Assert.AreEqual(0, result.Value.AllChanges);

			io.WriteFile("file1.txt", "some text");

			result = await cmd.GetStatusAsync(ct);
			Assert.AreEqual(1, result.Value.AllChanges);
			Assert.AreEqual(1, result.Value.Added);
			Assert.IsNotNull(result.Value.Files.FirstOrDefault(f => f.FilePath == "file1.txt"));

			io.DeleteFile("file1.txt");
			result = await cmd.GetStatusAsync(ct);
			Assert.IsTrue(result.IsOk);
			Assert.AreEqual(0, result.Value.AllChanges);
		}

		[Test]
		public async Task TestStatusAfterCommit()
		{
			await git.InitRepoAsync();
			io.WriteFile("file1.txt", "text1");
			await git.CommitAllChangesAsync("Message1");

			io.WriteFile("file1.txt", "text21");
			io.WriteFile("file2.txt", "text22");
			R<GitStatus2> result = await cmd.GetStatusAsync(ct);
			Assert.AreEqual(2, result.Value.AllChanges);

		}


		[Test]
		public async Task TestGetStatus()
		{
			await git.InitRepoAsync();

			GitStatus2 status = await git.GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
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
			R result = await cmd.UndoAllUncommittedAsync(ct);
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
			result = await cmd.UndoAllUncommittedAsync(ct);
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
				R<IReadOnlyList<string>> result = await cmd.UndoAllUncommittedAsync(ct);
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
			R<IReadOnlyList<string>> result = await cmd.UndoAllUncommittedAsync(ct);
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
			result = await cmd.UndoAllUncommittedAsync(ct);
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
		public async Task TestUndoFileAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "some text 1");
			io.WriteFile("file2.txt", "some text 2");
			await git.CommitAllChangesAsync("Message 1");

			io.WriteFile("file1.txt", "some text 12");
			io.DeleteFile("file2.txt");
			io.WriteFile("file3.txt", "some text 32");
			io.CreateDir("Folder1");
			io.WriteFile("Folder1/file4.txt", "some text 42");
			Assert.AreEqual(true, io.ExistsFile("Folder1/file4.txt"));
			Assert.AreEqual("some text 42", io.ReadFile("Folder1/file4.txt"));
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.Modified);
			Assert.AreEqual(1, status.Deleted);
			Assert.AreEqual(2, status.Added);

			R result = await cmd.UndoUncommittedFileAsync("file1.txt", ct);
			Assert.AreEqual(true, result.IsOk);
			Assert.AreEqual("some text 1", io.ReadFile("file1.txt"));

			result = await cmd.UndoUncommittedFileAsync("file2.txt", ct);
			Assert.AreEqual(true, result.IsOk);
			Assert.AreEqual("some text 2", io.ReadFile("file2.txt"));

			result = await cmd.UndoUncommittedFileAsync("file3.txt", ct);
			Assert.AreEqual(true, result.IsOk);
			Assert.AreEqual(false, io.ExistsFile("file3.txt"));

			result = await cmd.UndoUncommittedFileAsync("Folder1/file4.txt", ct);
			Assert.AreEqual(true, result.IsOk);
			Assert.AreEqual(false, io.ExistsFile("Folder1/file4.txt"));
		}
	}
}
