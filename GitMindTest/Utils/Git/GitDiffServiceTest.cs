using System.IO;
using System.Threading.Tasks;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitDiffServiceTest : GitTestBase<IGitDiffService2>
	{
		[Test]
		public async Task TestDiffCommitAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "text1");
			io.WriteFile("file2.txt", "text2");
			GitCommit commit1 = await git.CommitAllChangesAsync("Message1");

			io.WriteFile("file1.txt", "text12");
			io.WriteFile("file2.txt", "text22");
			GitCommit commit2 = await git.CommitAllChangesAsync("Message2");

			R<string> result = await cmd.GetCommitDiffAsync(commit1.Sha.Sha, ct);
			Assert.AreEqual(true, result.IsOk);

			CommitDiff diff = await git.ParsePatchAsync(commit1.Sha, result.Value);
			Assert.IsNotNullOrEmpty(diff.LeftText);
			Assert.IsNotNullOrEmpty(diff.RightText);

			result = await cmd.GetCommitDiffAsync(commit2.Sha.Sha, ct);
			Assert.AreEqual(true, result.IsOk);

			CommitDiff diff2 = await git.ParsePatchAsync(commit1.Sha, result.Value);
			Assert.IsNotNullOrEmpty(diff2.LeftText);
			Assert.IsNotNullOrEmpty(diff2.RightText);
		}

		[Test]
		public async Task TestDiffFiletAsync()
		{
			GitDiffParser diffParser = new GitDiffParser();

			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "text1");
			io.WriteFile("file2.txt", "text2");
			GitCommit commit1 = await git.CommitAllChangesAsync("Message1");

			io.WriteFile("file1.txt", "text12");
			io.WriteFile("file2.txt", "text22");
			GitCommit commit2 = await git.CommitAllChangesAsync("Message2");

			R<string> result1 = await cmd.GetFileDiffAsync(commit1.Sha.Sha, "file1.txt", ct);
			Assert.AreEqual(true, result1.IsOk);

			// "file1.txt" ####
			CommitDiff diff = await diffParser.ParseAsync(commit1.Sha, result1.Value, false, false);
			Assert.IsNullOrEmpty(File.ReadAllText(diff.LeftPath));
			Assert.AreEqual("text1\r\n", File.ReadAllText(diff.RightPath));

			R<string> result2 = await cmd.GetFileDiffAsync(commit2.Sha.Sha, "file2.txt", ct);
			Assert.AreEqual(true, result2.IsOk);

			// "file2.txt" ####
			CommitDiff diff2 = await diffParser.ParseAsync(commit1.Sha, result2.Value, false, false);
			Assert.AreEqual("text2", File.ReadAllText(diff2.LeftPath));
			Assert.AreEqual("text22\r\n", File.ReadAllText(diff2.RightPath));
		}


		[Test]
		public async Task TestDiffDeletedFiletAsync()
		{
			GitDiffParser diffParser = new GitDiffParser();

			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "text1");
			GitCommit commit1 = await git.CommitAllChangesAsync("Message1");

			io.DeleteFile("file1.txt");
			GitCommit commit2 = await git.CommitAllChangesAsync("Message2");

			R<string> result1 = await cmd.GetFileDiffAsync(commit1.Sha.Sha, "file1.txt", ct);
			Assert.AreEqual(true, result1.IsOk);

			CommitDiff diff1 = await diffParser.ParseAsync(commit1.Sha, result1.Value, false, false);
			Assert.IsNullOrEmpty(File.ReadAllText(diff1.LeftPath));
			Assert.AreEqual("text1\r\n", File.ReadAllText(diff1.RightPath));

			R<string> result2 = await cmd.GetFileDiffAsync(commit2.Sha.Sha, "file1.txt", ct);
			Assert.AreEqual(true, result2.IsOk);

			CommitDiff diff2 = await diffParser.ParseAsync(commit2.Sha, result2.Value, false, false);
			Assert.AreEqual("text1\r", File.ReadAllText(diff2.LeftPath));
			Assert.IsNullOrEmpty(File.ReadAllText(diff2.RightPath));
		}


		[Test, Explicit]
		public async Task TestDiffUncommittedAsync()
		{
			GitDiffParser diffParser = new GitDiffParser();

			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "line1\nline2\nline3\n");
			io.WriteFile("file2.txt", "line1\nline2\nline3\n");
			await git.CommitAllChangesAsync("Message1");

			io.WriteFile("file1.txt", "line1\nline22\nline3\n");
			io.DeleteFile("file2.txt");
			io.WriteFile("file3.txt", "line1\nline2\nline3\n");

			R<string> result = await cmd.GetUncommittedDiffAsync(ct);
			CommitDiff diff = await diffParser.ParseAsync(null, result.Value, true, false);
			string d1 = File.ReadAllText(diff.LeftPath);
			string d2 = File.ReadAllText(diff.RightPath);
			Assert.IsNotNullOrEmpty(result.Value);

			result = await cmd.GetUncommittedFileDiffAsync("file1.txt", ct);
			Assert.IsNotNullOrEmpty(result.Value);



			//io.WriteFile("file1.txt", "line1\nline22\nline3\n");
			//io.DeleteFile("file2.txt");
			//io.WriteFile("file3.txt", "line1\nline2\nline3\n");

			//result = await cmd.GetUncommittedDiffAsync(ct);
			//Assert.IsNotNullOrEmpty(result.Value);

			result = await cmd.GetUncommittedFileDiffAsync("file1.txt", ct);
			Assert.IsNotNullOrEmpty(result.Value);

			result = await cmd.GetUncommittedFileDiffAsync("file2.txt", ct);
			Assert.IsNotNullOrEmpty(result.Value);

			result = await cmd.GetUncommittedFileDiffAsync("file3.txt", ct);
			Assert.IsNotNullOrEmpty(result.Value);
		}
	}
}