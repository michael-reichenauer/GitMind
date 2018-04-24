using System.Threading;
using GitMind.Utils;
using GitMind.Utils.OsSystem;
using NUnit.Framework;


namespace GitMindTest.Utils.OsSystem
{
	[TestFixture]
	public class SystemIdleTest
	{
		[Test, Explicit]
		public void TestIdle()
		{
			for (int i = 0; i < 30; i++)
			{
				Log.Debug($"Time: {SystemIdle.GetLastInputIdleTimeSpan()}");
				Thread.Sleep(1000);
			}
		}
	}
}