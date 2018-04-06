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
			string path = DirCreateTmp();

			R result = await gitCmd.InitAsync(path, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test]
		public async Task TestInitBareAsync()
		{
			string path = DirCreateTmp();

			R result = await gitCmd.InitBareAsync(path, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test]
		public async Task TestCloneAsync()
		{
			string originPath = DirCreateTmp();

			R result = await gitCmd.InitBareAsync(originPath, ct);
			Assert.IsTrue(result.IsOk);

			string path = DirCreateTmp();
			result = await gitCmd.CloneAsync(originPath, path, null, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test, Explicit]
		public async Task TestCloneGitMindRepoAsync()
		{
			void Progress(string text) => Log.Debug($"Progress: {text}");

			string path = DirCreateTmp();
			R result = await gitCmd.CloneAsync(
				"https://github.com/michael-reichenauer/GitMind.git", path, Progress, ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test]
		public async Task TestInitRepoAsync()
		{
			await InitRepoAsync();
		}

		[Test]
		public async Task TestCloneRepoAsync()
		{
			await CloneRepoAsync();
		}
	}
}