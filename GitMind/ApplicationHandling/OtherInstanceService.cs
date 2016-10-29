using System;
using GitMind.MainWindowViews;
using GitMind.Utils;


namespace GitMind.ApplicationHandling
{
	internal class OtherInstanceService
	{
		public bool IsActivatedOtherInstance(string workingFolder)
		{
			string id = MainWindowIpcService.GetId(workingFolder);
			using (IpcRemotingService ipcRemotingService = new IpcRemotingService())
			{
				if (!ipcRemotingService.TryCreateServer(id))
				{
					// Another GitMind instance for that working folder is already running, activate that.
					var args = Environment.GetCommandLineArgs();
					ipcRemotingService.CallService<MainWindowIpcService>(id, service => service.Activate(args));
					return true;
				}
			}

			return false;
		}
	}
}