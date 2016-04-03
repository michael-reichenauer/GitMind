using GitMind.Utils;
using NUnit.Framework;


namespace GitMindTest.Utils
{
	[TestFixture]
	public class ProcessTest
	{
		[Test]
		public void Test()
		{
			Cmd cmd = new Cmd();

			string output;
			Assert.AreEqual(0, cmd.Run("git", "version", out output));

			Assert.That(output, Is.StringStarting("git version 2."));
		}
	}
}
