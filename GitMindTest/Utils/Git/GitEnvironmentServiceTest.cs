using System.IO;
using System.Threading.Tasks;
using GitMind.Utils.Git;
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
			Assert.AreEqual("2.16.2.windows.1", version);
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
			await cmd.InstallGitAsync();
		}


	}
}