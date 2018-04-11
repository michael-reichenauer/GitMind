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
	public class GitLogTest : GitTestBase<IGitLogService>
	{
		[Test]
		public async Task TestLog()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "some text 1");
			await git.CommitAllChangesAsync("Message 1");

			io.WriteFile("file2.txt", "some text 2");
			await git.CommitAllChangesAsync("Message 2");

			R<IReadOnlyList<GitCommit>> log = await cmd.GetLogAsync(ct);
			Assert.AreEqual(2, log.Value.Count);
			Assert.AreEqual("Message 2", log.Value[0].Message);
			Assert.AreEqual("Message 1", log.Value[1].Message);

			List<GitCommit> log2 = new List<GitCommit>();
			R result = await cmd.GetLogAsync(c => log2.Add(c), ct);
			Assert.AreEqual(2, log2.Count);
			Assert.AreEqual("Message 2", log2[0].Message);
			Assert.AreEqual("Message 1", log2[1].Message);

			R<GitCommit> commit = await cmd.GetCommitAsync(log.Value[0].Sha.Sha, ct);
			Assert.AreEqual("Message 2", commit.Value.Message);

			commit = await cmd.GetCommitAsync(log.Value[1].Sha.Sha, ct);
			Assert.AreEqual("Message 1", commit.Value.Message);
		}

		[Test, Explicit]
		public async Task TestGetPartialLog()
		{
			await git.InitRepoAsync();

			for (int i = 0; i < 10; i++)
			{
				io.WriteFile("file.txt", $"some text {i}");
				await git.CommitAllChangesAsync($"Message {i}");
			}

			R<IReadOnlyList<GitCommit>> log = await cmd.GetLogAsync(ct);
			Assert.AreEqual(10, log.Value.Count);
			Assert.AreEqual("Message 9", log.Value[0].Message);
			Assert.AreEqual("Message 8", log.Value[1].Message);

			int count = 0;
			CancellationTokenSource cts = new CancellationTokenSource();
			List<GitCommit> log2 = new List<GitCommit>();
			R result = await cmd.GetLogAsync(c =>
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


		[Test]
		public async Task TestGetCommitMessageLog()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "some text 1");
			string message1 = "Message 1\n\nSome body text l1\nome body text l2";
			GitCommit commit1 = await git.CommitAllChangesAsync(message1);

			io.WriteFile("file2.txt", "some text 2");
			string message2 = "Message 2";
			GitCommit commit2 = await git.CommitAllChangesAsync(message2);

			io.WriteFile("file2.txt", "some text 3");
			string message3 = "Message 2\nsome short body";
			GitCommit commit3 = await git.CommitAllChangesAsync(message3);

			R<string> message21 = await cmd.GetCommitMessageAsync(commit1.Sha.Sha, ct);
			Assert.AreEqual(message1, message21.Value);

			R<string> message22 = await cmd.GetCommitMessageAsync(commit2.Sha.Sha, ct);
			Assert.AreEqual(message2, message22.Value);

			R<string> message23 = await cmd.GetCommitMessageAsync(commit3.Sha.Sha, ct);
			Assert.AreEqual(message3, message23.Value);
		}
	}
}