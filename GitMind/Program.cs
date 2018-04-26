using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using GitMind.ApplicationHandling;
using GitMind.Common;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind
{
	public class Program
	{
		private readonly DependencyInjection dependencyInjection = new DependencyInjection();


		[STAThread]
		public static void Main()
		{
			Log.Debug(GetStartLineText());

			Program program = new Program();
			program.Run();
		}


		private void Run()
		{
			// Add handler and logging for unhandled exceptions
			ExceptionHandling.HandleUnhandledException();

			// Make external assemblies that GitMind depends on available, when needed (extracted)
			ActivateExternalDependenciesResolver();

			// Activate dependency injection support
			dependencyInjection.RegisterDependencyInjectionTypes();

			var askPassService = dependencyInjection.Resolve<IGitAskPassService>();

			if (askPassService.TryHandleRequest())
			{
				return;  // The Ask Pass service handled this request
			}

			// Start application
			App application = dependencyInjection.Resolve<App>();
			ExceptionHandling.HandleDispatcherUnhandledException();
			application.InitializeComponent();
			application.Run();
		}


		private static void ActivateExternalDependenciesResolver()
		{
			AssemblyResolver.Activate();
			CommandLine commandLine = new CommandLine();

			if (commandLine.IsInstall || commandLine.IsUninstall)
			{
				// LibGit2 requires native git2.dll, which should not be extracted during install/uninstall
				// Since that would create a dll next to the setup file.
				AssemblyResolver.DoNotExtractLibGit2();
			}
		}


		private static string GetStartLineText()
		{
			string version = GetProgramVersion();

			string[] args = Environment.GetCommandLineArgs();
			string argsText = string.Join("','", args);

			return $"Start version: {version}, args: '{argsText}'";
		}


		private static string GetProgramVersion()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			return fvi.FileVersion;
		}
	}
}