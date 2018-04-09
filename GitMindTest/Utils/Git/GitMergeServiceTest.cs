using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitMergeServiceTest : GitTestBase<IGitMergeService2>
	{
		[Test]
		public async Task TestMergeAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "Text 1");
			await git.CommitAllChangesAsync("Message 1");

			await git.BrancheAsync("branch1");
			io.WriteFile("file1.txt", "Text on branch 1");
			await git.CommitAllChangesAsync("Message 1");

			await git.CheckoutAsync("master");
			Assert.AreEqual("Text 1", io.ReadFile("file1.txt"));

			R result = await cmd.MergeAsync("branch1", ct);
			Assert.AreEqual(true, result.IsOk);
			status = await git.GetStatusAsync();
			Assert.AreEqual(true, status.OK);

			Assert.AreEqual("Text on branch 1", io.ReadFile("file1.txt"));
		}

		[Test]
		public async Task TestMergeWithConflictsAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "Text 1");
			io.WriteFile("file2.txt", "Text 2");
			io.WriteFile("file3.txt", "Text 3");
			io.WriteFile("file4.txt", "Text 4");
			io.WriteFile("file5.txt", "Text 5");
			await git.CommitAllChangesAsync("Initial addon master");

			await git.BrancheAsync("branch1");
			io.WriteFile("file1.txt", "Text on branch 1");
			io.DeleteFile("file2.txt");
			io.WriteFile("file3.txt", "Text on branch 3");
			io.WriteFile("file4.txt", "Text on branch 4");
			io.DeleteFile("file5.txt");
			io.WriteFile("file6.txt", "Text on branch 6");  // Added branch
			await git.CommitAllChangesAsync("Message 1 on branch 1");

			await git.CheckoutAsync("master");
			io.WriteFile("file1.txt", "Text on master 1");
			io.WriteFile("file2.txt", "Text on master 2");
			io.DeleteFile("file3.txt");
			// skip file 4
			io.DeleteFile("file5.txt");
			io.WriteFile("file6.txt", "Text on master 6"); // added on master
			await git.CommitAllChangesAsync("Message 2 on master");

			R result = await cmd.MergeAsync("branch1", ct);
			Assert.AreEqual(true, result.IsOk);
			status = await git.GetStatusAsync();
			Assert.AreEqual(1, status.Modified);
			Assert.AreEqual(4, status.Conflicted);

			GitConflicts conflicts = await git.GetConflictsAsync();
			Assert.AreEqual(false, conflicts.OK);
			Assert.AreEqual(4, conflicts.Count);
		}
	}
}