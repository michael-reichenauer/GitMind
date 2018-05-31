using System;
using System.Threading.Tasks;
using System.Windows;
using GitMind.ApplicationHandling;
using GitMind.Features.Commits;
using GitMind.Utils;
using GitMind.Utils.Ipc;


namespace GitMind.MainWindowViews
{
	internal class MainWindowIpcService : IpcService
	{
		private readonly ICommitsService commitsService;

		private static readonly string InstanceId = "0000278d-5c40-4973-aad9-1c33196fd1a2";


		public MainWindowIpcService(ICommitsService commitsService)
		{
			this.commitsService = commitsService;
		}


		public static string GetId(string workingFolder) =>
			InstanceId + Uri.EscapeDataString(workingFolder.ToLower());


		public void Activate(string[] args)
		{
			CommandLine commandLine = new CommandLine();

			Application.Current.Dispatcher.InvokeAsync(() =>
			{
				Application.Current.MainWindow.WindowState = WindowState.Minimized;
				Application.Current.MainWindow.Activate();
				Application.Current.MainWindow.WindowState = WindowState.Normal;

				if (commandLine.IsCommitCommand(args ?? new string[0]))
				{
					Log.Usage("Activated and commit");
					commitsService.CommitChangesAsync().RunInBackground();
				}
				else
				{
					Log.Usage("Activated");
				}
			});
		}
	}
}