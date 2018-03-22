using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;
using GitMind.Utils.OsSystem;
using GitMindTest.AutoMocking;
using NUnit.Framework;


namespace GitMindTest.Utils.Git.Private
{
	[TestFixture]
	public class GitCmdTest
	{
		private readonly CancellationToken ct = CancellationToken.None;

		[Test, Explicit]
		public async Task TestCmd()
		{
			using (AutoMock am = new AutoMock()
				.RegisterNamespaceOf<IGitCmd>()
				.RegisterNamespaceOf<ICmd2>())
			{
				IGitCmd gitCmd = am.Resolve<IGitCmd>();

				GitResult result = await gitCmd.RunAsync("version", ct);

				Assert.AreEqual(0, result.ExitCode);
				Assert.That(result.Output, Is.StringMatching(@"git version \d\.\d+.*windows"));
			}
		}
	}
}