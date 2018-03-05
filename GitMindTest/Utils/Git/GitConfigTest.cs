using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.OsSystem;
using GitMindTest.AutoMocking;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitConfigTest
	{
		private readonly CancellationToken ct = CancellationToken.None;

		[Test, Explicit]
		public async Task Test()
		{
			string ToText(IReadOnlyDictionary<string, string> config) =>
				string.Join("\n", config.Select(p => $"{p.Key}={p.Value}"));

			using (AutoMock am = new AutoMock()
				.RegisterNamespaceOf<IGitVersion>()
				.RegisterNamespaceOf<ICmd2>()
				.RegisterSingleInstance(new WorkingFolderPath(@"C:\Work Files\GitMind")))
			{
				IGitConfig gitCmd = am.Resolve<IGitConfig>();

				IReadOnlyDictionary<string, string> config = await gitCmd.GetAsync(ct);
				Log.Debug($"Config:\n{ToText(config)}");
			}
		}
	}
}