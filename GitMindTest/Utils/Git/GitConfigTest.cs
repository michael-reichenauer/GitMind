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
	public class GitConfigTest : GitTestBase<IGitConfig>
	{
		[Test, Explicit]
		public async Task Test()
		{
			string ToText(IReadOnlyList<GitSetting> c) =>
				string.Join("\n", c.Select(p => p.ToString()));

			IReadOnlyList<GitSetting> config = await gitCmd.GetAsync(ct);
			Log.Debug($"Config:\n{ToText(config)}");
		}
	}
}