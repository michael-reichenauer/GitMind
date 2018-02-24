using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git;
using GitMind.Utils.OsSystem;
using GitMindTest.AutoMocking;
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
			using (AutoMock am = new AutoMock()
				.RegisterNamespaceOf<IGitLog>()
				.RegisterNamespaceOf<ICmd2>())
			{
				IGitCmd gitCmd = am.Resolve<IGitCmd>();
				var result = await gitCmd.DoAsync("log --all --pretty=\"%H|%ai|%ci|%an|%P|%s\"", ct);

			}
		}
	}
}