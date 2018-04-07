using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git.Private;
using GitMind.Utils.OsSystem;
using NUnit.Framework;


namespace GitMindTest.Utils.Git.Private
{
	[TestFixture]
	public class GitCmdTest : GitTestBase<IGitCmdService>
	{
		[Test]
		public async Task TestCmd()
		{
			R<CmdResult2> result = await cmd.RunAsync("version", ct);

			Assert.AreEqual(0, result.Value.ExitCode);
			Assert.That(result.Value.Output, Is.StringMatching(@"git version \d\.\d+.*windows"));
		}
	}
}