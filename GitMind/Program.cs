using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core.Activators.Reflection;
using GitMind.ApplicationHandling;
using GitMind.Common;
using GitMind.Utils;


namespace GitMind
{
	public class Program
	{
		private IContainer container;


		[STAThread]
		public static void Main()
		{
			Log.Debug(GetStartlineText());

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
			container = RegisterDependencyInjectionTypes();

			// Start application
			App application = container.Resolve<App>();
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
				AssemblyResolver.DoNotExtractLibGit2();
			}
		}


		private static IContainer RegisterDependencyInjectionTypes()
		{
			try
			{
				ContainerBuilder builder = new ContainerBuilder();

				// Need to make Autofac find also "internal" constructors e.g. windows dialogs
				DefaultConstructorFinder constructorFinder = new DefaultConstructorFinder(
					type => type.GetConstructors(
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

				Assembly executingAssembly = Assembly.GetExecutingAssembly();

				// Register single instance types
				builder.RegisterAssemblyTypes(executingAssembly)
					.Where(IsSingleInstance)
					.FindConstructorsWith(constructorFinder)
					.AsSelf()
					.AsImplementedInterfaces()
					.SingleInstance()
					.OwnedByLifetimeScope();

				// Register non single instance types
				builder.RegisterAssemblyTypes(executingAssembly)
					.Where(t => !IsSingleInstance(t))
					.FindConstructorsWith(constructorFinder)
					.AsSelf()
					.AsImplementedInterfaces()
					.OwnedByLifetimeScope();

				return builder.Build();
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to register types {e}");
				throw;
			}
		}


		private static bool IsSingleInstance(Type type)
		{
			// All types that are marked with the "SingleInstance" attribute
			return type.GetCustomAttributes(false).FirstOrDefault(
				obj => obj.GetType().Name == "SingleInstanceAttribute") != null;
		}


		private static string GetStartlineText()
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