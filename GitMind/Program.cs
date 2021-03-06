﻿using System;
using GitMind.ApplicationHandling;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind
{
	/// <summary>
	/// Contains the main entry point for the program
	/// </summary>
	public class Program
	{
		private readonly DependencyInjection dependencyInjection = new DependencyInjection();


		[STAThread]
		public static void Main()
		{
			Log.Debug($"Start version: {ProgramInfo.Version}, args: '{ProgramInfo.ArgsText}'");

			Program program = new Program();
			program.Run();
		}


		/// <summary>
		/// The program instance main entry point
		/// </summary>
		private void Run()
		{
			// Add handler for unhandled exceptions
			ExceptionHandling.HandleUnhandledException();

			// Make dependency assemblies available, when needed (extract from resources)
			AssemblyResolver.Activate();

			// Activate dependency injection support
			dependencyInjection.RegisterDependencyInjectionTypes();

			if (IsAskGitPasswordRequest())
			{
				// The git ask ask password service handled this request
				return;
			}

			// Start application
			App application = dependencyInjection.Resolve<App>();
			ExceptionHandling.HandleDispatcherUnhandledException();
			application.InitializeComponent();
			application.Run();
		}


		private bool IsAskGitPasswordRequest()
		{
			var askPassService = dependencyInjection.Resolve<IGitAskPassService>();
			return askPassService.TryHandleRequest();
		}
	}
}