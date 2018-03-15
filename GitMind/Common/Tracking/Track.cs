using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Utils;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
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
			if (Settings.Get<Options>().DisableErrorAndUsageReporting)
			{
				Log.Info("Disabled usage and error reporting");
				return;
			}

			Log.Info("Enabled usage and error reporting");
			Tc = new TelemetryClient();

			Tc.InstrumentationKey = GetInstrumentationKey();
			Tc.Context.User.Id = GetTrackId();
			SetInternalNodeName();
			Tc.Context.Cloud.RoleInstance = Tc.Context.User.Id;
			Tc.Context.User.UserAgent = $"{ProgramPaths.ProgramName}/{ProgramPaths.GetRunningVersion()}";
			Tc.Context.Session.Id = Guid.NewGuid().ToString();
			Tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
			Tc.Context.Component.Version = GetProgramVersion();

			//var builder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;
			//builder.Use((next) => new TelemetryTracer(next));
			//builder.Build();
		}


		public static void StartProgram()
		{
			if (!isStarted)
			{
				isStarted = true;

				Event("Start-Program");
			}
		}


		public static void ExitProgram()
		{
			if (isStarted)
			{
				isStarted = false;
				Event("Exit-Program");
			}

			Tc?.Flush();
		}


		public static void Event(string eventName)
		{
			Event(eventName);

		}


		public static void Event(string eventName, string message)
		{
			Log.Debug($"{eventName}: {message}");

			if (message != null)
			{
				Tc.TrackEvent(eventName, new Dictionary<string, string> { { "Message", message } });
			}
			else
			{
				Tc?.TrackEvent(eventName);
			}
		}


		public static void Dependency(
			string commandName, string target, TimeSpan duration, bool isSuccess)
		{

			if (Uri.TryCreate(target, UriKind.Absolute, out Uri uri))
			{
				target = uri.Host;
			}

			Log.Debug($"{commandName}, {target}, {duration}, {isSuccess}");
			Tc?.TrackDependency(target, commandName, DateTimeOffset.Now - duration, duration, isSuccess);

			//Tc?.TrackDependency(
			//	target, target, target, 
			//	commandName, DateTimeOffset.Now - duration, duration, isSuccess.ToString(), isSuccess);
		}


		public static void Info(string message)
		{
			Trace(message, SeverityLevel.Information);
		}


		public static void Warn(string message)
		{
			Trace(message, SeverityLevel.Warning);
		}


		public static void Error(string message)
		{
			Trace(message, SeverityLevel.Error);
		}

#pragma warning disable CS3001 // Argument type is not CLS-compliant
		public static void Trace(string message, SeverityLevel level)
#pragma warning restore CS3001 // Argument type is not CLS-compliant
		{
			Log.Debug($"{level}: {message}");
			Tc?.TrackTrace(message, level);
		}

		public static void Request(string requestName)
		{
			Log.Debug($"{requestName}");
			Tc?.TrackRequest(new RequestTelemetry(
				requestName, DateTime.Now, TimeSpan.FromMilliseconds(1), "", true));
		}


		public static void Command(string name)
		{
			Event($"Command-{name}");
		}


		public static void Window(string window)
		{
			Log.Debug($"{window}");
			Tc?.TrackPageView(window);
		}


		public static void Exception(Exception e, string msg)
		{
			Log.Debug($"{e.GetType()}, {msg}");
			Tc?.TrackException(e, new Dictionary<string, string> { { "Message", msg } });
			Tc?.Flush();
		}


		private static string GetInstrumentationKey()
		{
			string currentInstancePath = ProgramPaths.GetCurrentInstancePath();

			if (currentInstancePath != null &&
					(!currentInstancePath.StartsWithOic(ProgramPaths.GetProgramFolderPath())
				|| IsSetupFile()))
			{
				Log.Info("Using production metrics");
				return "33982a8a-1da0-42c0-9d0a-8a159494c847";
			}

			Log.Info("Using test metrics");
			return "77fee87e-bd1e-4341-ac5b-0a65c3e567bb";
		}


		private static bool IsSetupFile()
		{
			return Path.GetFileNameWithoutExtension(ProgramPaths.GetCurrentInstancePath())
				.StartsWithOic("GitMindSetup");
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

			Log.Debug($"Track id: {trackId}");
			return trackId;
		}


		private static string GetProgramVersion()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			return fvi.FileVersion;
		}


		private static void SetInternalNodeName()
		{
			PropertyInfo internalProperty = typeof(TelemetryContext).GetProperty("Internal",
				BindingFlags.NonPublic | BindingFlags.Instance);

			InternalContext internalContext = (InternalContext)internalProperty.GetValue(Tc.Context);

			internalContext.NodeName = Tc.Context.User.Id;
		}


		private class TelemetryTracer : ITelemetryProcessor
		{
			private readonly ITelemetryProcessor next;


			public TelemetryTracer(ITelemetryProcessor next)
			{
				this.next = next;
			}


			public void Process(ITelemetry item)
			{
				// Use Application Insight serializer
				byte[] bytes = JsonSerializer.Serialize(new[] { item }, false);
				string text = Encoding.UTF8.GetString(bytes);

				// Indent the json for to make readable 
				object obj = Json.As<object>(text);
				string indentedText = Json.AsJson(obj);

				Log.Debug($"Telemetry: {item.GetType().Name}\n{indentedText}");
				next.Process(item);
			}
		}
	}
}