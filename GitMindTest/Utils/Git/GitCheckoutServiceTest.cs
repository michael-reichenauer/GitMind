using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitCheckoutServiceTest : GitTestBase<IGitCheckoutService>
	{
		[Test]
		public async Task TestCheckoutAsync()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "text1");
			await git.CommitAllChangesAsync("Message1");

			// Try checkout non exist branch, will get result OK, but false as value
			R<bool> result = await cmd.TryCheckoutAsync("branch1", ct);
			Assert.AreEqual(true, result.IsOk);
			Assert.AreEqual(false, result.Value);

			await git.BrancheAsync("branch1", false);

			// Try checkout branch1, will succeed and value is true
			result = await cmd.TryCheckoutAsync("branch1", ct);
			Assert.AreEqual(true, result.IsOk);
			Assert.AreEqual(true, result.Value);
		}
	}
}