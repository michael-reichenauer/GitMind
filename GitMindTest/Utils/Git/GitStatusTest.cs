using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitStatusTest : GitTestBase<IGitStatus>
	{
		[Test, Explicit]
		public async Task Test()
		{
			R<Status2> status = await gitCmd.GetStatusAsync(ct);
			Assert.IsTrue(status.IsOk);
		}
	}
}