using System.Threading.Tasks;


// ReSharper disable once CheckNamespace
namespace System.Windows.Threading
{
	public static class DispatcherExtensions
	{
		public static void Delay(this Dispatcher dispatcher, TimeSpan delay, Action action) => 
			Task.Delay(delay).ContinueWith(_ => { dispatcher.Invoke(action); }).RunInBackground();
	}
}