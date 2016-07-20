using System.Diagnostics;


namespace GitMind.Utils.UI
{
	internal class WpfBindingTraceListener : TraceListener
	{
		public override void Write(string message)
		{
		}

		public override void WriteLine(string message)
		{
			Log.Warn($"WPF binding error:\n{message}");
			//Debugger.Break();
		}


		public static void Register()
		{
			PresentationTraceSources.Refresh();
			//PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
			PresentationTraceSources.DataBindingSource.Listeners.Add(new WpfBindingTraceListener());
			PresentationTraceSources.DataBindingSource.Switch.Level
				= SourceLevels.Warning | SourceLevels.Error;
		}
	}
}