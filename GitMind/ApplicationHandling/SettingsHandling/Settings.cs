using System;
using System.IO;
using GitMind.Utils;


namespace GitMind.ApplicationHandling.SettingsHandling
{
	public static class Settings
	{
		public static void EnsureExists<T>()
		{
			// A Get will ensure that the file exists
			T settings = Get<T>();
			Set(settings);
		}

		public static void Edit<T>(Action<T> editAction)
		{
			try
			{
				T settings = Get<T>();
				editAction(settings);
				Set(settings);
			}
			catch (Exception e)
			{
				Log.Exception(e, "Error editing the settings");
			}
		}

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

			if (ParentFolderExists(path))
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
				Log.Exception(e, "Failed to create json");
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
					Log.Exception(e, "Failed to parse json");
				}
			}

			T defaultObject = Activator.CreateInstance<T>();
			if (ParentFolderExists(path))
			{
				if (json == null)
				{
					// Initial use of this settings file, lets store default
					json = Json.AsJson(defaultObject);
					WriteFileText(path, json);
				}
			}

			return defaultObject;
		}


		private static bool ParentFolderExists(string path)
		{
			string parentFolderPath = Path.GetDirectoryName(path);
			return Directory.Exists(parentFolderPath);
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
				Log.Exception(e, $"Failed to read file {path}");
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
				Log.Exception(e, $"Failed to write file {path}");
			}
		}


		private static string GetProgramSettingsPath<T>()
		{
			return Path.Combine(ProgramInfo.DataFolderPath, typeof(T).Name + ".json");
		}


		private static string GetWorkFolderSettingsPath(string workingFolder)
		{
			return Path.Combine(workingFolder, ".git", "GitMind.Setting.json");
		}
	}
}