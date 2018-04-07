using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitConfigTest : GitTestBase<IGitConfigService>
	{
		[Test, Explicit]
		public async Task Test()
		{
			string ToText(IReadOnlyList<GitSetting> c) =>
				string.Join("\n", c.Select(p => p.ToString()));

			R<IReadOnlyList<GitSetting>> config = await cmd.GetAsync(ct);
			Log.Debug($"Config:\n{ToText(config.Value)}");
		}
	}
}