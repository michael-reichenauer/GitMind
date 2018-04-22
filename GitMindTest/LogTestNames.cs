using System;
using GitMind.Utils;
using GitMindTest;
using NUnit.Framework;


[assembly: LogTestNames]


namespace GitMindTest
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class |
									AttributeTargets.Interface | AttributeTargets.Assembly,
		AllowMultiple = true)]
	public class LogTestNames : Attribute, ITestAction
	{
		public void BeforeTest(TestDetails testDetails)
		{
			string fixture = testDetails.Fixture?.GetType()?.FullName;
			if (fixture != null)
			{
				Log.Debug($"########### Before test: {fixture}.{testDetails.Method.Name}");
			}
		}


		public void AfterTest(TestDetails testDetails)
		{
			string fixture = testDetails.Fixture?.GetType()?.FullName;
			if (fixture != null)
			{
				Log.Debug($"    ########### After test: {fixture}.{testDetails.Method.Name}");
			}
		}

		public ActionTargets Targets => ActionTargets.Test | ActionTargets.Suite;
	}
}