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
			string ToText(IReadOnlyDictionary<string, string> c) =>
				string.Join("\n", c.Select(p => $"{p.Key}={p.Value}"));

			IReadOnlyDictionary<string, string> config = await gitCmd.GetAsync(ct);
			Log.Debug($"Config:\n{ToText(config)}");
		}
	}
}