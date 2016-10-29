using System;
using System.Windows;
using GitMind.ApplicationHandling.Installation.Private;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Utils;


namespace GitMind.MainWindowViews
{
	internal class MainWindowIpcService : IpcService
	{
		private readonly MainWindowViewModel mainWindowViewModel;


		public MainWindowIpcService(MainWindowViewModel mainWindowViewModel)
		{
			this.mainWindowViewModel = mainWindowViewModel;
		}


		public static string GetId(string workingFolder) =>
			ProgramPaths.ProductGuid + Uri.EscapeDataString(workingFolder);


		public void Activate(string[] args)
		{
			CommandLine commandLine = new CommandLine(args);

			Application.Current.Dispatcher.InvokeAsync(() =>
			{
				Application.Current.MainWindow.WindowState = WindowState.Minimized;
				Application.Current.MainWindow.Activate();
				Application.Current.MainWindow.WindowState = WindowState.Normal;

				if (commandLine.IsCommit)
				{
					Log.Usage("Activated and commit");
					if (mainWindowViewModel.RepositoryViewModel.CommitCommand.CanExecute())
					{
						mainWindowViewModel.RepositoryViewModel.CommitCommand.Execute();
					}
				}
				else
				{
					Log.Usage("Activated");
				}
			});
		}
	}
}