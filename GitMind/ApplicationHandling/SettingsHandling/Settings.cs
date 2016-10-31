using System;
using System.IO;
using GitMind.Utils;


namespace GitMind.ApplicationHandling.SettingsHandling
{
	public static class Settings
	{
		public static T Get<T>()
		{
			string path = GetProgramSettingsPath<T>();
			return ReadAs<T>(path);
		}


		public static void Set<T>(T setting)
		{
			string path = GetProgramSettingsPath<T>();
			WriteAs(path, setting);
		}


		public static WorkFolderSettings GetWorkFolderSetting(string workingFolder)
		{
			string path = GetWorkFolderSettingsPath(workingFolder);

			return ReadAs<WorkFolderSettings>(path);
		}


		public static void SetWorkFolderSetting(string workingFolder, WorkFolderSettings settings)
		{
			string path = GetWorkFolderSettingsPath(workingFolder);

			if (Directory.Exists(Path.GetDirectoryName(path)))
			{
				WriteAs(path, settings);
			}
		}


		private static void WriteAs<T>(string path, T obj)
		{
			try
			{
				string json = Json.AsJson(obj);
				WriteFileText(path, json);
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Error($"Failed to create json {e}");
			}
		}


		private static T ReadAs<T>(string path)
		{
			string json = TryReadFileText(path);
			if (json != null)
			{
				try
				{
					return Json.As<T>(json);
				}
				catch (Exception e) when (e.IsNotFatal())
				{
					Log.Error($"Failed to parse json {e}");
				}
			}

			T defaultObject = Activator.CreateInstance<T>();
			if (json == null)
			{
				// Initial use of this settings file, lets store default
				json = Json.AsJson(defaultObject);
				WriteFileText(path, json);
			}

			return defaultObject;
		}


		private static string TryReadFileText(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					return File.ReadAllText(path);
				}
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn($"Failed to read file {path}, {e}");
			}

			return null;
		}


		private static void WriteFileText(string path, string text)
		{
			try
			{
				File.WriteAllText(path, text);
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn($"Failed to write file {path}, {e}");
			}
		}


		private static string GetProgramSettingsPath<T>()
		{
			return Path.Combine(ProgramPaths.DataFolderPath, typeof(T).Name + ".json");
		}


		private static string GetWorkFolderSettingsPath(string workingFolder)
		{
			return Path.Combine(workingFolder, ".git", "GitMind.Setting.json");
		}
	}
}