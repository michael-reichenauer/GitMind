using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Utils.Git;
using GitMind.Utils.OsSystem;
using GitMindTest.AutoMocking;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitFetchTest
	{
		private readonly CancellationToken ct = CancellationToken.None;

		[Test, Explicit]
		public async Task Test()
		{
			using (AutoMock am = new AutoMock()
				.RegisterNamespaceOf<IGitFetch>()
				.RegisterNamespaceOf<ICmd2>()
				.RegisterSingleInstance(new WorkingFolderPath(@"C:\Work Files\GitMind")))
			{
				IGitFetch gitFetch = am.Resolve<IGitFetch>();

				await gitFetch.FetchAsync(ct);
			}
		}
	}
}