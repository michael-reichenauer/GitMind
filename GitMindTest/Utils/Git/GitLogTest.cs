using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitLogTest : GitTestBase<IGitLog>
	{
		[Test]
		public async Task TestLog()
		{
			await InitRepoAsync();

			WriteFile("file1.txt", "some text 1");
			await CommitAllChangesAsync("Message 1");

			WriteFile("file2.txt", "some text 2");
			await CommitAllChangesAsync("Message 2");

			R<IReadOnlyList<GitCommit>> log = await gitCmd.GetLogAsync(ct);
			Assert.AreEqual(2, log.Value.Count);
			Assert.AreEqual("Message 2", log.Value[0].Message);
			Assert.AreEqual("Message 1", log.Value[1].Message);

			List<GitCommit> log2 = new List<GitCommit>();
			R result = await gitCmd.GetLogAsync(c => log2.Add(c), ct);
			Assert.AreEqual(2, log2.Count);
			Assert.AreEqual("Message 2", log2[0].Message);
			Assert.AreEqual("Message 1", log2[1].Message);

			R<GitCommit> commit = await gitCmd.GetCommitAsync(log.Value[0].Sha.Sha, ct);
			Assert.AreEqual("Message 2", commit.Value.Message);

			commit = await gitCmd.GetCommitAsync(log.Value[1].Sha.Sha, ct);
			Assert.AreEqual("Message 1", commit.Value.Message);
		}

		[Test, Explicit]
		public async Task Test()
		{
			await InitRepoAsync();

			for (int i = 0; i < 10; i++)
			{
				WriteFile("file.txt", $"some text {i}");
				await CommitAllChangesAsync($"Message {i}");
			}

			R<IReadOnlyList<GitCommit>> log = await gitCmd.GetLogAsync(ct);
			Assert.AreEqual(10, log.Value.Count);
			Assert.AreEqual("Message 9", log.Value[0].Message);
			Assert.AreEqual("Message 8", log.Value[1].Message);

			int count = 0;
			CancellationTokenSource cts = new CancellationTokenSource();
			List<GitCommit> log2 = new List<GitCommit>();
			R result = await gitCmd.GetLogAsync(c =>
			{
				log2.Add(c);
				Log.Debug($"Added {c}");

				if (++count == 5)
				{
					Log.Debug("Canceling");
					cts.Cancel();
				}
			},
			cts.Token);

			Assert.IsTrue(result.IsOk);

			Assert.AreEqual(5, log2.Count);
			Assert.AreEqual("Message 9", log2[0].Message);
			Assert.AreEqual("Message 8", log2[1].Message);
		}
	}
}