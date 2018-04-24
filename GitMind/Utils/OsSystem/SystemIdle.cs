using System;
using System.Runtime.InteropServices;
using System.Threading;


namespace GitMind.Utils.OsSystem
{
	public class SystemIdle
	{
		public static void TriggerOnIdle(TimeSpan idleTime, Action action, CancellationToken ct)
		{
			void CheckIdle(object state)
			{
				if (DateTime.UtcNow - GetLastInputUtcTime() > idleTime)
				{
					action();
				}
			}

			Timer timer = new Timer(CheckIdle);
			timer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
			ct.Register(() => timer.Dispose());
		}


		public static TimeSpan GetLastInputIdleTimeSpan() => DateTime.UtcNow - GetLastInputUtcTime();


		public static DateTime GetLastInputUtcTime()
		{
			var lastInputInfo = new Native.LASTINPUTINFO();
			lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

			Native.GetLastInputInfo(ref lastInputInfo);

			DateTime lastInput = DateTime.UtcNow.AddMilliseconds(
				-(Environment.TickCount - lastInputInfo.dwTime));
			return lastInput;
		}



		private static class Native
		{
			[DllImport("User32.dll")]
			public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);


			[StructLayout(LayoutKind.Sequential)]
			internal struct LASTINPUTINFO
			{
				public uint cbSize;
				public uint dwTime;
			}
		}
	}
}