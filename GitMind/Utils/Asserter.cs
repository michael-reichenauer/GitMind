using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using GitMind.Common;


namespace GitMind.Utils
{
	public static class Asserter
	{
		public static void NotNull(
			object instance,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Requires(instance != null, memberName, sourceFilePath, sourceLineNumber);
		}


		public static void Requires(
			bool predicate,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			if (!predicate)
			{
				Fail("assert", memberName, sourceFilePath, sourceLineNumber);
			}
		}

		public static Exception FailFast(
			string errorMessage,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			return Fail(errorMessage, memberName, sourceFilePath, sourceLineNumber);
		}


		public static Exception FailFast(
			Error error,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			return Fail(error.Message, memberName, sourceFilePath, sourceLineNumber);
		}


		private static Exception Fail(
			 string error, string memberName, string sourceFilePath, int sourceLineNumber)
		{
			StackTrace stackTrace = new StackTrace(true);

			string message =
				$"Fail {error} at\n{sourceFilePath}({sourceLineNumber}) {memberName}\n\n{stackTrace}";

			Exception exception = new InvalidOperationException(message);

			ExceptionHandling.Shutdown("Assert failed", exception);
			return exception;
		}
	}
}
