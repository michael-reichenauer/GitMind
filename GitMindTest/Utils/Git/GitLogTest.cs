using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.OsSystem;
using GitMindTest.AutoMocking;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitLogTest
	{
		private readonly CancellationToken ct = CancellationToken.None;

		[Test, Explicit]
		public async Task Test()
		{
			using (AutoMock am = new AutoMock()
				.RegisterNamespaceOf<IGitLog>()
				.RegisterNamespaceOf<ICmd2>())
			{
				IGitLog gitLog = am.Resolve<IGitLog>();

				IReadOnlyList<LogCommit> result = await gitLog.GetAsync(ct);

				Log.Debug($"Log contained {result.Count} Commits");
			}
		}
	}
}