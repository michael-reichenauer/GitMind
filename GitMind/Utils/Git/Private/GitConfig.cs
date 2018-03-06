using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitConfig : IGitConfig
	{
		private static readonly string ConfigListArgs = "config --list";
		private static readonly char[] EqualChar = "=".ToCharArray();

		private readonly IGitCmd gitCmd;


		public GitConfig(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public bool TryGet(string name, out GitSetting setting)
		{
			string ToText(IReadOnlyList<GitSetting> c) =>
				string.Join("\n", c.Select(p => p.ToStringAll()));


			IReadOnlyList<GitSetting> settings =
				Task.Run(() => GetAsync(CancellationToken.None)).Result;

			Log.Debug($"Config:\n{ToText(settings)}");
			setting = settings.FirstOrDefault(s => s.Name == name);

			return setting != null;
		}


		public async Task<IReadOnlyList<GitSetting>> GetAsync(CancellationToken ct)
		{
			CmdResult2 result = await gitCmd.RunAsync(ConfigListArgs, ct);
			result.ThrowIfError("Failed to get config list");

			Dictionary<string, List<string>> settings = new Dictionary<string, List<string>>();

			foreach (string line in result.OutputLines)
			{
				string settingText = line.Trim();
				if (!string.IsNullOrEmpty(settingText))
				{
					string[] setting = settingText.Split(EqualChar);

					string key = setting[0];
					string value = setting[1];

					if (!settings.TryGetValue(key, out List<string> values))
					{
						values = new List<string>();
						settings[key] = values;
					}

					if (!(values.Any() && values.Last() == value))
					{
						values.Add(value);
					}
				}
			}

			return settings.Select(s => new GitSetting(s.Key, s.Value)).ToList();
		}
	}
}