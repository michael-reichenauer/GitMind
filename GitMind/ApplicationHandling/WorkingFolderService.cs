//using System;
//using System.IO;
//using GitMind.ApplicationHandling.SettingsHandling;
//using GitMind.ApplicationHandling.Testing;
//using GitMind.Utils;


//namespace GitMind.ApplicationHandling
//{
//	internal class WorkingFolderService
//	{
//		private readonly ICommandLine commandLine;

//		private readonly Lazy<string> lazyWorkingFolder;


//		public WorkingFolderService(ICommandLine commandLine)
//		{
//			this.commandLine = commandLine;
//			lazyWorkingFolder = new Lazy<string>(GetWorkingFolder);
//		}


//	}
//}