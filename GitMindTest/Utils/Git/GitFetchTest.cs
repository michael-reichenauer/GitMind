﻿using System.Threading.Tasks;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitFetchTest : GitTestBase<IGitFetch>
	{
		[Test, Explicit]
		public async Task Test()
		{
			GitResult result = await gitCmd.FetchAsync(ct);
			Assert.IsTrue(result.IsOk);
		}
	}
}