using System;
using System.Runtime.InteropServices;


namespace GitMind.Utils.UI
{
	public static class FileUtil
	{
		public static void DeleteFileAtReboot(string path)
		{
			if (!NativeMethods.MoveFileEx(path, null, MoveFileFlags.DelayUntilReboot))
			{
				Log.Warn($"Unable to schedule '{path}' for deletion");
			}
		}


		[Flags]
		private enum MoveFileFlags
		{
			None = 0,
			ReplaceExisting = 1,
			CopyAllowed = 2,
			DelayUntilReboot = 4,
			WriteThrough = 8,
			CreateHardlink = 16,
			FailIfNotTrackable = 32,
		}

		private static class NativeMethods
		{
			[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
			public static extern bool MoveFileEx(
					string lpExistingFileName,
					string lpNewFileName,
					MoveFileFlags dwFlags);
		}
	}
}