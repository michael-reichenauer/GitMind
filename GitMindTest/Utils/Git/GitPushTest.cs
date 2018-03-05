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
	public class GitPushTest
	{
		private readonly CancellationToken ct = CancellationToken.None;

		[Test, Explicit]
		public async Task Test()
		{
			using (AutoMock am = new AutoMock()
				.RegisterNamespaceOf<ICmd2>()
				.RegisterNamespaceOf<IGitVersion>()
				.RegisterSingleInstance(new WorkingFolderPath(@"C:\Work Files\GitMind")))
			{
				IGitPush gitFetch = am.Resolve<IGitPush>();

				await gitFetch.PushAsync(ct);
			}
		}
	}
}