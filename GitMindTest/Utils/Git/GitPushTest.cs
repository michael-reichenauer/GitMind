using System.Threading.Tasks;
using GitMind.Utils.Git;
using GitMindTest.Utils.Git.Private;
using NUnit.Framework;


namespace GitMindTest.Utils.Git
{
	[TestFixture]
	public class GitPushTest : GitTestBase<IGitPush>
	{
		[Test, Explicit]
		public async Task Test()
		{
			GitResult result = await gitCmd.PushAsync(ct);
			Assert.IsTrue(result.IsOk);
		}


		[Test, Explicit]
		public void TestUri()
		{
			//string targetUrl = "https://michael.reichenauer@gmail.com@github.com";
			//int i1 = targetUrl.IndexOf('@');
			//if (i1 > 1 && i1 < targetUrl.Length - 1)
			//{
			//	int i2 = targetUrl.IndexOf('@', i1 + 1);
			//	if (i2 != -1)
			//	{
			//		StringBuilder sb = new StringBuilder(targetUrl);
			//		sb[i1] = '_';
			//		theString = sb.ToString();
			//	}
			//	else
			//	{
			//		i1 = -1;
			//	}

			//}

			//Assert.IsTrue(Uri.TryCreate(targetUrl, UriKind.Absolute, out Uri targetUri));
		}
	}
}