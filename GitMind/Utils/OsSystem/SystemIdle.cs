using System;
using System.Runtime.InteropServices;


namespace GitMind.Utils.OsSystem
{
	public class SystemIdle
	{
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