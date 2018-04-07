using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitRepoTest : GitTestBase<IGitRepoService>
	{
		[Test]
		public async Task TestInitAsync()
		{
			string path = io.CreateTmpDir();

			R result = await cmd.InitAsync(path, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test]
		public async Task TestInitBareAsync()
		{
			string path = io.CreateTmpDir();

			R result = await cmd.InitBareAsync(path, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test]
		public async Task TestCloneAsync()
		{
			string originPath = io.CreateTmpDir();

			R result = await cmd.InitBareAsync(originPath, ct);
			Assert.IsTrue(result.IsOk);

			string path = io.CreateTmpDir();
			result = await cmd.CloneAsync(originPath, path, null, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test, Explicit]
		public async Task TestCloneGitMindRepoAsync()
		{
			void Progress(string text) => Log.Debug($"Progress: {text}");

			string path = io.CreateTmpDir();
			R result = await cmd.CloneAsync(
				"https://github.com/michael-reichenauer/GitMind.git", path, Progress, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test]
		public async Task TestInitRepoAsync()
		{
			await git.InitRepoAsync();
		}

		[Test]
		public async Task TestCloneRepoAsync()
		{
			await git.CloneRepoAsync();
		}
	}
}