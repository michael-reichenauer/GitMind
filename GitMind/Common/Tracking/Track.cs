using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.ApplicationInsights;
using Microsoft.Win32;


namespace GitMind.Common.Tracking
{
	public static class Track
	{
		private static readonly string TrackIdFileName = "e34c91d8-0a3b-4d3e-9571-373335e57836";

		private static readonly TelemetryClient Tc;
		private static bool isStarted = false;

		static Track()
		{
			Tc = new TelemetryClient();

			Tc.InstrumentationKey = "33982a8a-1da0-42c0-9d0a-8a159494c847";
			Tc.Context.User.Id = GetTrackId();
			Tc.Context.Session.Id = Guid.NewGuid().ToString();
			Tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
		}

		public static void StartProgram()
		{
			if (!isStarted)
			{
				isStarted = true;

				Tc.TrackEvent(
					"Start-Program",
					new Dictionary<string, string> { { "Version", GetProgramVersion() } });
			}
		}

		public static void ExitProgram()
		{
			if (isStarted)
			{
				isStarted = false;
				Tc.TrackEvent("Exit-Program");
			}

			Tc.Flush();
		}


		public static void Event(string eventName)
		{
			Tc.TrackEvent(eventName);
		}


		public static void Command(string command)
		{
			Tc.TrackEvent("Command-" + command);
		}


		public static void Window(string window)
		{
			Tc.TrackPageView(window);
		}


		public static void Exception(Exception e)
		{
			Tc.TrackException(e);
		}


		private static string GetTrackId()
		{
			string trackIdPath = Path.Combine(Path.GetTempPath(), TrackIdFileName);

			string userId;
			if (File.Exists(trackIdPath))
			{
				userId = File.ReadAllText(trackIdPath);
			}
			else
			{
				userId = Guid.NewGuid().ToString();
				userId = (string)Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\GitMind", "TrackId", userId);
				File.WriteAllText(trackIdPath, userId);
			}

			// Backup track id in registry in case temp file is deleted
			Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\GitMind", "TrackId", userId);

			return userId;
		}


		private static string GetProgramVersion()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			return fvi.FileVersion;
		}
	}
}