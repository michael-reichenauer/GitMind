using System;


namespace GitMind.Utils
{
	/// <summary>
	/// The IPC Remoting service base class. 
	/// On the server side, instances of classes, which inherits this class will receive IPC calls.
	/// On client side, proxy instances, based on that type, are used to make IPC calls.
	/// </summary>
	internal class IpcService : MarshalByRefObject
	{
		// Remoting Object's ease expires after every 5 minutes by default. We need to override the
		// InitializeLifetimeService class to ensure that lease never expires.
		public override object InitializeLifetimeService() => null;
	}
}