using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
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
			Tc.Context.Component.Version = GetProgramVersion();
		}

		public static void StartProgram()
		{
			if (!isStarted)
			{
				isStarted = true;

				Tc.TrackEvent("Start-Program");
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


		public static void Event(string eventName, string message)
		{
			Tc.TrackEvent(eventName, new Dictionary<string, string> { { "Message", message } });
		}


		public static void Command(
			string command, 
			DateTime startTime, 
			TimeSpan duration, 
			string exitCode, 
			bool isSuccess)
		{
			Tc.TrackRequest(new RequestTelemetry(command, startTime, duration, exitCode, isSuccess));
		}


		public static void Window(string window)
		{
			Tc.TrackPageView(window);
		}


		public static void Exception(Exception e, string msg)
		{
			Tc.TrackException(e, new Dictionary<string, string> { { "Message", msg } });
			Tc.Flush();
		}
		

		private static string GetTrackId()
		{
			string trackIdPath = Path.Combine(Path.GetTempPath(), TrackIdFileName);

			string trackId = null;
			if (File.Exists(trackIdPath))
			{
				trackId = File.ReadAllText(trackIdPath);
			}

			if (string.IsNullOrWhiteSpace(trackId))
			{
				// No track id in temp file, lets check registry
				string regId = (string)Registry.GetValue(
					"HKEY_CURRENT_USER\\SOFTWARE\\GitMind", "TrackId", null);
				if (!string.IsNullOrWhiteSpace(regId))
				{
					// Using the track id in the registry
					trackId = regId;
				}
				else
				{
					trackId = Guid.NewGuid().ToString();
				}

				File.WriteAllText(trackIdPath, trackId);
			}

			// Backup track id in registry in case temp file is deleted
			Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\GitMind", "TrackId", trackId);

			return trackId;
		}


		private static string GetProgramVersion()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			return fvi.FileVersion;
		}
	}
}