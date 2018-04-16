using System.IO;
using System.Threading.Tasks;
using GitMind.Features.Diffing.Private;
using GitMind.Git;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
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
			GitDiffParser diffParser = new GitDiffParser();

			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "text1");
			io.WriteFile("file2.txt", "text2");
			GitCommit commit1 = await git.CommitAllChangesAsync("Message1");

			io.WriteFile("file1.txt", "text12");
			io.WriteFile("file2.txt", "text22");
			GitCommit commit2 = await git.CommitAllChangesAsync("Message2");

			R<string> result = await cmd.GetCommitDiffAsync(commit1.Sha.Sha, ct);
			Assert.AreEqual(true, result.IsOk);

			CommitDiff diff = await diffParser.ParseAsync(commit1.Sha, result.Value, true, false);
			Assert.IsNotNullOrEmpty(File.ReadAllText(diff.LeftPath));
			Assert.IsNotNullOrEmpty(File.ReadAllText(diff.RightPath));

			result = await cmd.GetCommitDiffAsync(commit2.Sha.Sha, ct);
			Assert.AreEqual(true, result.IsOk);

			CommitDiff diff2 = await diffParser.ParseAsync(commit1.Sha, result.Value, true, false);
			Assert.IsNotNullOrEmpty(File.ReadAllText(diff2.LeftPath));
			Assert.IsNotNullOrEmpty(File.ReadAllText(diff2.RightPath));
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

			R<string> result = await cmd.GetFileDiffAsync(commit1.Sha.Sha, "file1.txt", ct);
			Assert.AreEqual(true, result.IsOk);

			CommitDiff diff = await diffParser.ParseAsync(commit1.Sha, result.Value, false, false);
			Assert.IsNotNullOrEmpty(File.ReadAllText(diff.LeftPath));
			Assert.IsNotNullOrEmpty(File.ReadAllText(diff.RightPath));

			result = await cmd.GetFileDiffAsync(commit2.Sha.Sha, "file2.txt", ct);
			Assert.AreEqual(true, result.IsOk);

			CommitDiff diff2 = await diffParser.ParseAsync(commit1.Sha, result.Value, false, false);
			Assert.IsNotNullOrEmpty(File.ReadAllText(diff2.LeftPath));
			Assert.IsNotNullOrEmpty(File.ReadAllText(diff2.RightPath));
		}

	}
}