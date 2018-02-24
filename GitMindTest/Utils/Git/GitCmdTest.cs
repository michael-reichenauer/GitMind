using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git;
using GitMind.Utils.OsSystem;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitCmdTest
	{
		private CancellationToken ct = CancellationToken.None;

		[Test]
		public async Task Test()
		{
			GitCmd gitCmd = new GitCmd(new Cmd2());

			var result = await gitCmd.DoAsync("log --all --pretty=\"%H|%ai|%ci|%an|%P|%s\"", ct);
		}
	}
}