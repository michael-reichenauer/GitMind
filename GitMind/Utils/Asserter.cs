using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;


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
			Error error,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Fail(error.Message, memberName, sourceFilePath, sourceLineNumber);

			return new InvalidOperationException();
		}


		private static void Fail(
			 string error, string memberName, string sourceFilePath, int sourceLineNumber)
		{
			StackTrace stackTrace = new StackTrace(true);

			string message =
				$"Fail {error} at\n{sourceFilePath}({sourceLineNumber}) {memberName}\n\n{stackTrace}";

			ShowError(message);

			CloseProgram(message);
			
		}


		private static void CloseProgram(string message)
		{
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}

			Environment.FailFast(message);
		}


		private static void ShowError(string message)
		{
			Log.Error(message);

			MessageBox.Show(
				Application.Current.MainWindow,
				message,
				"GitMind - Asserter",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}
	}
}
