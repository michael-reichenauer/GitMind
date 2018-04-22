using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitRepoServiceTest : GitTestBase<IGitRepoService>
	{

		[Test, Explicit]
		public async Task Test()
		{
			await git.InitRepoAsync();

			io.WriteFile("file1.txt", "Text 1");
			await git.CommitAllChangesAsync("Message 1");

		}

		[Test, Explicit]
		public void TestClean()
		{
			io.CleanTempDirs();
			////Process.Start("RD", $"/S /Q \"{io.GetTempBaseDirPath()}\"");
			//Process process = new Process();
			//// Configure the process using the StartInfo properties.
			//process.StartInfo.FileName = "RD";
			//process.StartInfo.Arguments = $"/S /Q \"{io.GetTempBaseDirPath()}\"";
			//process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
			//process.StartInfo.UseShellExecute = true;
			//process.Start();
			//process.WaitForExit();
		}

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



		[Test]
		public async Task TestClone2ReposAsync()
		{
			await git.CloneRepoAsync();

			io.WriteFile("file1.txt", "Text 1");
			await git.CommitAllChangesAsync("Message 1");
			await git.PushAsync();

			await git2.CloneRepoAsync(git.OriginUri);
			branches = await git2.GetBranchesAsync();
			Assert.AreEqual(2, branches.Count);
			Assert.AreEqual(0, branches[0].BehindCount);
			Assert.AreEqual(0, branches[0].AheadCount);
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