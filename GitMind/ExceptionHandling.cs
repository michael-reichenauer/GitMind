using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GitMind.Utils;


namespace GitMind
{
	internal static class ExceptionHandling
	{
		private static bool hasDisplayedErrorMessageBox;
		private static bool hasFailed;

		private static readonly List<SuppressItems> suppressItems = new List<SuppressItems>();



		public static void Init()
		{
			// Add the event handler for handling UI thread exceptions to the event.		
			Application.Current.DispatcherUnhandledException += (s, e) =>
				HandleException("dispatcher exception", e.Exception);

			// Add the event handler for handling non-UI thread exceptions to the event. 
			AppDomain.CurrentDomain.UnhandledException += (s, e) =>
			HandleException("app domain exception", e.ExceptionObject as Exception);

			// Log exceptions that hasn't been handled when a Task is finalized.
			TaskScheduler.UnobservedTaskException += (s, e) =>
				HandleException("unobserved task exception", e.Exception);


			// Set the handler for unhandled exceptions on a Task that is marked as FailOnFaulted.
			// FailOnFaultedTaskExtensions.FailOnFaultedTask += TaskFailOnFaulted;

			// Register handler for all exceptions as they are being thrown
			// AppDomain.CurrentDomain.FirstChanceException += FirstChanceException;

			RegisterSuppressionAttributes();
		}


		private static void RegisterSuppressionAttributes()
		{
			Type[] types = Assembly.GetExecutingAssembly().GetTypes();
			foreach (Type type in types)
			{
				foreach (MemberInfo member in type.GetMembers(
					BindingFlags.Instance | BindingFlags.Public |
					BindingFlags.NonPublic | BindingFlags.Static))
				{
					IEnumerable<AcceptingExceptionsAttribute> attributes = member.GetCustomAttributes()
						.OfType<AcceptingExceptionsAttribute>();

					foreach (var attribute in attributes)
					{
						string location = attribute.ExternalLocation ??
							$"{member.DeclaringType?.FullName}.{member.Name}";

						suppressItems.Add(new SuppressItems(location, attribute.ExceptionType));
					}
				}
			}
		}


		///// <summary>
		///// Called for each exception thrown this application domain.
		///// </summary>
		//private static void FirstChanceException(object sender, FirstChanceExceptionEventArgs args)
		//{
		//	StackTrace stackTrace = new StackTrace(1, true);

		//	if (SuppressedException(stackTrace, args.Exception))
		//	{
		//		return;
		//	}


		//	HandleException($"exception:\n{args.Exception}\n\nThrown:\n{stackTrace}", args.Exception);
		//}


		//private static bool SuppressedException(StackTrace stackTrace, Exception e)
		//{
		//	Type exceptionType = e.GetType();

		//	string stackTraceText = stackTrace.ToString();

		//	foreach (SuppressItems item in suppressItems)
		//	{
		//		if (stackTraceText.Contains(item.Location) && item.ExceptionType == exceptionType)
		//		{
		//			// Log.Error($"First chance exception suppressed:\n{e}\nthrown via:\n {item.Location}");
		//			return true;
		//		}
		//	}



		//	return false;
		//}


		//private static void TaskFailOnFaulted(object sender, FailOnFaultedTaskEventArgs e)
		//{
		//	HandleException("TaskFailOnFaulted", e.FaultedTask.Exception);
		//}


		private static void HandleException(string errorType, Exception exception)
		{
			if (hasFailed)
			{
				return;
			}

			hasFailed = true;

			string errorMessage = $"Unhandled {errorType}\n{exception}";
			Log.Error(errorMessage);

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


		public static void Shutdown(string errorMessage, Exception e)
		{
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

			MessageBox.Show(
				Application.Current.MainWindow,
				errorMessage,
				"GitMind - Unhandled Exception",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}


		private static Dispatcher GetApplicationDispatcher() =>
			Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;


		private class SuppressItems
		{
			public string Location { get; }
			public Type ExceptionType { get; }

			public SuppressItems(string location, Type exceptionType)
			{
				Location = location;
				ExceptionType = exceptionType;
			}
		}
	}
}