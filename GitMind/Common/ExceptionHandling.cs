using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GitMind.Common.MessageDialogs;
using GitMind.Utils;


namespace GitMind.Common
{
	internal static class ExceptionHandling
	{
		private static bool hasDisplayedErrorMessageBox;
		private static bool hasFailed;


		public static void Init()
		{
			// Add the event handler for handling UI thread exceptions to the event		
			Application.Current.DispatcherUnhandledException += (s, e) =>
				HandleException("dispatcher exception", e.Exception);

			// Add the event handler for handling non-UI thread exceptions to the event. 
			AppDomain.CurrentDomain.UnhandledException += (s, e) =>
			HandleException("app domain exception", e.ExceptionObject as Exception);

			// Log exceptions that hasn't been handled when a Task is finalized.
			TaskScheduler.UnobservedTaskException += (s, e) =>
				HandleException("unobserved task exception", e.Exception);
		}


		private static void HandleException(string errorType, Exception exception)
		{
			if (hasFailed)
			{
				return;
			}

			hasFailed = true;

			string errorMessage = $"Unhandled {errorType}";
			
			if (Debugger.IsAttached)
			{
				// NOTE: If you end up here a task resulted in an unhandled exception
				Debugger.Break();
			}
			else
			{
				Shutdown(errorMessage, exception);
			}
		}


		public static void Shutdown(string message, Exception e)
		{
			string errorMessage = $"{message}:\n{e}";
			Log.Error(errorMessage);

			var dispatcher = GetApplicationDispatcher();
			if (dispatcher.CheckAccess())
			{
				ShowExceptionDialog(errorMessage, e);
			}
			else
			{
				dispatcher.Invoke(() => ShowExceptionDialog(errorMessage, e));
			}

			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}

			Environment.FailFast(errorMessage, e);
		}


		private static void ShowExceptionDialog(string errorMessage, Exception e)
		{
			if (hasDisplayedErrorMessageBox)
			{
				return;
			}

			hasDisplayedErrorMessageBox = true;

			Message.ShowError(
				Application.Current.MainWindow, errorMessage, "GitMind - Unhandled Exception");
		}


		private static Dispatcher GetApplicationDispatcher() =>
			Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
	}
}