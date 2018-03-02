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
	public class GitVersionTest
	{
		private readonly CancellationToken ct = CancellationToken.None;

		[Test, Explicit]
		public async Task Test()
		{
			using (AutoMock am = new AutoMock()
				.RegisterNamespaceOf<IGitVersion>()
				.RegisterNamespaceOf<ICmd2>()
				.RegisterSingleInstance(new WorkingFolderPath(@"C:\Work Files\GitMind")))
			{
				IGitVersion gitFetch = am.Resolve<IGitVersion>();

				string version = await gitFetch.GetAsync(ct);
				Assert.AreEqual("git version 2.16.2.windows.1", version);
			}
		}
	}
}