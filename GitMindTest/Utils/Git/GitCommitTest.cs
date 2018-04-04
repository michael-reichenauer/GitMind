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
	public class GitCommitTest : GitTestBase<IGitCommitService2>
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
	}
}