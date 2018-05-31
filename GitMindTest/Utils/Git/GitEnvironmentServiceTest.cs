using System.IO;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitEnvironmentServiceTest : GitTestBase<IGitEnvironmentService>
	{
		[Test]
		public void TestVersion()
		{
			string version = cmd.TryGetGitVersion();
			Assert.AreEqual(GitEnvironmentService.GitVersion, version);
		}


		[Test]
		public void TestGitCorePath()
		{
			string path = cmd.TryGetGitCorePath();
			Assert.IsNotNull(path);
		}

		[Test]
		public void TestGitCmdPath()
		{
			string path = cmd.TryGetGitCmdPath();
			Assert.IsNotNull(path);
		}


		[Test]
		public async Task TestWorkingFolderRootPath()
		{
			await git.InitRepoAsync();
			string workingFolder = io.WorkingFolder;
			io.CreateDir("Folder1");
			io.CreateDir("Folder1/SubFolder");

			Assert.AreEqual(workingFolder, cmd.TryGetWorkingFolderRoot(
				io.FullPath("Folder1/SubFolder")));

			Assert.AreEqual(workingFolder, cmd.TryGetWorkingFolderRoot(
				Path.Combine(workingFolder, ".git")));
		}


		[Test]
		public async Task TestInstallAsync()
		{
			await cmd.InstallGitAsync(text => { Log.Debug(text); });

			string version = cmd.TryGetGitVersion();
			Assert.AreEqual(GitEnvironmentService.GitVersion, version);
		}
	}
}