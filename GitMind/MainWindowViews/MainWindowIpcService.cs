using System;
using System.Windows;
using GitMind.Installation.Private;
using GitMind.Settings;
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

			Log.Warn($"args: {string.Join(",", args)}");

			Application.Current.Dispatcher.InvokeAsync(() =>
			{
				Application.Current.MainWindow.Activate();
				Application.Current.MainWindow.WindowState = WindowState.Normal;

				if (commandLine.IsCommit)
				{
					Log.Warn("Commit cmd");
					if (mainWindowViewModel.RepositoryViewModel.CommitCommand.CanExecute())
					{
						mainWindowViewModel.RepositoryViewModel.CommitCommand.Execute();
					}
				}
			});
		}
	}
}