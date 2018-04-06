﻿using System.Linq;
using System.Threading.Tasks;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitStatusTest : GitTestBase<IGitStatusService2>
	{
		[Test]
		public async Task TestStatus()
		{
			await InitRepoAsync();

			R<Status2> status = await gitCmd.GetStatusAsync(ct);
			Assert.IsTrue(status.IsOk);
			Assert.AreEqual(0, status.Value.AllChanges);

			FileWrite("file1.txt", "some text");

			status = await gitCmd.GetStatusAsync(ct);
			Assert.AreEqual(1, status.Value.AllChanges);
			Assert.AreEqual(1, status.Value.Added);
			Assert.IsNotNull(status.Value.Files.FirstOrDefault(f => f.FilePath == "file1.txt"));

			FileDelete("file1.txt");
			status = await gitCmd.GetStatusAsync(ct);
			Assert.IsTrue(status.IsOk);
			Assert.AreEqual(0, status.Value.AllChanges);
		}


		[Test]
		public async Task TestGetStatus()
		{
			await InitRepoAsync();

			Status2 status = await GetStatusAsync();
			Assert.AreEqual(0, status.AllChanges);
		}
	}
}