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
			Assert(instance != null, memberName, sourceFilePath, sourceLineNumber);
		}


		public static void Requires(
			bool predicate,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Assert(predicate, memberName, sourceFilePath, sourceLineNumber);
		}


		private static void Assert(
			bool predicate, string memberName, string sourceFilePath, int sourceLineNumber)
		{
			if (!predicate)
			{
				StackTrace stackTrace = new StackTrace(true);

				string message =
					$"Assert failed at\n{sourceFilePath}({sourceLineNumber}) {memberName}\n\n{stackTrace}";

				Log.Error(message);

				MessageBox.Show(
					Application.Current.MainWindow,
					message,
					"Asserter",
					MessageBoxButton.OK,
					MessageBoxImage.Error);

				if (Debugger.IsAttached)
				{
					Debugger.Break();
				}
				else
				{
					Debugger.Launch();
					Application.Current.Shutdown(-1);
				}
			}
		}


		public static Exception FailFast(Error error)
		{
			return FailFast(error.Message);
		}


		public static Exception FailFast(string error)
		{
			string message = $"Failed: {error}, at:\n {new StackTrace()}";
			Log.Error(message);

			MessageBox.Show(
				Application.Current.MainWindow,
				message,
				"GitMind - Asserter",
				MessageBoxButton.OK,
				MessageBoxImage.Error);

			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			else
			{
				Application.Current.Shutdown(-1);
			}

			return new InvalidOperationException();
		}
	}
}
