using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GitMind.Settings;
using GitMind.Utils;


namespace GitMind.Common
{
	internal static class ExceptionHandling
	{
		private static readonly ICmd cmd = new Cmd();
		private static bool hasDisplayedErrorMessageBox;
		private static bool hasFailed;
		private static bool hasShutdown;

		public static void Init()
		{
			// Add the event handler for handling UI thread exceptions to the event		
			Application.Current.DispatcherUnhandledException += (s, e) =>
			{
				HandleException("dispatcher exception", e.Exception);
				e.Handled = true;
			};

			// Add the event handler for handling non-UI thread exceptions to the event. 
			AppDomain.CurrentDomain.UnhandledException += (s, e) =>
			{
				HandleException("app domain exception", e.ExceptionObject as Exception);
			};

			// Log exceptions that hasn't been handled when a Task is finalized.
			TaskScheduler.UnobservedTaskException += (s, e) =>
			{
				HandleException("unobserved task exception", e.Exception);
				e.SetObserved();
			};
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
			if (hasShutdown)
			{
				return;
			}

			hasShutdown = true;

			string errorMessage = $"{message}:\n{e}";
			Log.Error(errorMessage);

			var dispatcher = GetApplicationDispatcher();
			if (dispatcher.CheckAccess())
			{
				ShowExceptionDialog(e);
			}
			else
			{
				dispatcher.Invoke(() => ShowExceptionDialog(e));
			}

			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}

			Restart();

			Application.Current.Shutdown(0);
		}


		private static void ShowExceptionDialog(Exception e)
		{
			if (hasDisplayedErrorMessageBox)
			{
				return;
			}

			// Skipping message window for now, the error has been logged.
			hasDisplayedErrorMessageBox = true;
		}


		private static void Restart()
		{
			string targetPath = ProgramPaths.GetInstallFilePath();
			cmd.Start(targetPath, "");
		}


		private static Dispatcher GetApplicationDispatcher() =>
			Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
	}
}