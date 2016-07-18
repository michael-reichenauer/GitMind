using System;
using System.Collections.Generic;
using System.Windows;
using GitMind.Utils;
using Microsoft.Shell;


namespace GitMind
{
	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application, ISingleInstanceApp
	{
		private const string Unique = "E5485756-2F80-4921-87D9-E05196C6D770";


		[STAThread]
		public static void Main()
		{
			Log.Debug("Starting ...");

			var application = new App();

			application.InitializeComponent();
			application.Run();

			//if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
			//{
			//	var application = new App();

			//	application.InitializeComponent();
			//	application.Run();

			//	// Allow single instance code to perform cleanup operations
			//	SingleInstance<App>.Cleanup();
			//}
			//else
			//{
			//	Log.Debug("Second instance is closing");
			//}
		}


		protected override void OnStartup(StartupEventArgs e)
		{
			Log.Debug("Starting ...");
			base.OnStartup(e);
		}


		public bool SignalExternalCommandLineArgs(IList<string> args)
		{
			Log.Debug($"Got second instance argument ... '{string.Join(",", args)}'");
			Application.Current.MainWindow.Activate();
			return true;
		}
	}
}
