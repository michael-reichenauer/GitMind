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

			WriteFile("file1.txt", "some text");

			R<GitCommit> result = await gitCmd.CommitAllChangesAsync("Some message 1", ct);
			Assert.IsTrue(result.IsOk);

			Status2 status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);

			var files = await gitCmd.GetCommitFilesAsync(result.Value.Sha.Sha, ct);
			Assert.AreEqual(1, files.Value.Count);
			Assert.IsNotNull(files.Value.FirstOrDefault(f => f.FilePath == "file1.txt"));

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

			FileStream fileStream2 = File.OpenWrite(GetPath("file2.txt"));
			FileStream fileStream3 = File.OpenWrite(GetPath("file3.txt"));
			FileStream fileStream5 = File.OpenWrite(GetPath("file5.txt"));
			R result = await gitCmd.UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);

		}
	}
}