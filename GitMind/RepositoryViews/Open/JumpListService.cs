﻿using System.IO;
using System.Windows;
using System.Windows.Shell;
using GitMind.ApplicationHandling.SettingsHandling;


namespace GitMind.RepositoryViews.Open
{
	internal class JumpListService : IJumpListService
	{
		private static readonly int MaxTitleLength = 25;


		public void AddPath(string path)
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				return;
			}

			JumpList jumpList = JumpList.GetJumpList(Application.Current) ?? new JumpList();

			JumpTask jumpTask = new JumpTask
			{
				Title = GetTitle(path),
				ApplicationPath = ProgramPaths.GetInstallFilePath(),
				Arguments = GetOpenArguments(path),
				IconResourcePath = ProgramPaths.GetInstallFilePath(),
				Description = path
			};

			jumpList.ShowRecentCategory = true;
			JumpList.AddToRecentCategory(jumpTask);
			JumpList.SetJumpList(Application.Current, jumpList);
		}


		private static string GetTitle(string path)
		{
			string name = Path.GetFileNameWithoutExtension(path) ?? path;

			return name.Length < MaxTitleLength
				? name
				: name.Substring(0, MaxTitleLength) + "...";
		}


		private static string GetOpenArguments(string path) => $"/d:\"{path}\"";
	}
}