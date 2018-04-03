using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitStatusTest : GitTestBase<IGitStatus>
	{
		[Test]
		public async Task TestStatus()
		{
			R<Status2> status = await gitCmd.GetStatusAsync(ct);
			Assert.IsTrue(status.IsOk);
			Assert.AreEqual(0, status.Value.AllChanges);

			string path = Path.Combine(workingFolder, "file1.txt");
			File.WriteAllText(path, "some text");

			status = await gitCmd.GetStatusAsync(ct);
			Assert.AreEqual(1, status.Value.AllChanges);
			Assert.AreEqual(1, status.Value.Added);
			Assert.IsNotNull(status.Value.Files.FirstOrDefault(f => f.FilePath == "file1.txt")); ;
		}
	}
}