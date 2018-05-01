using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitConfigService : IGitConfigService
	{
		private static readonly string ConfigListArgs = "config --list";
		private static readonly char[] EqualChar = "=".ToCharArray();

		private readonly IGitCmdService gitCmdService;


		public GitConfigService(IGitCmdService gitCmdService)
		{
			this.gitCmdService = gitCmdService;
		}


		//public bool TryGet(string name, out GitSetting setting)
		//{
		//	IReadOnlyList<GitSetting> settings =
		//		Task.Run(() => GetAsync(CancellationToken.None)).Result;

		//	// Log.Debug($"Config:\n{ToText(settings)}");
		//	setting = settings.FirstOrDefault(s => s.Name == name);

		//	return setting != null;
		//}


		public async Task<R<IReadOnlyList<GitSetting>>> GetAsync(CancellationToken ct)
		{
			R<CmdResult2> result = await gitCmdService.RunAsync(ConfigListArgs, ct);
			if (result.IsFaulted)
			{
				return R.Error("Failed to get list of config values", result.Exception);
			}

			Dictionary<string, List<string>> settings = new Dictionary<string, List<string>>();

			foreach (string line in result.Value.OutputLines)
			{
				string settingText = line.Trim();

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

			return settings.Select(s => new GitSetting(s.Key, s.Value)).ToList();
		}


		private string ToText(IReadOnlyList<GitSetting> c) =>
			string.Join("\n", c.Select(p => p.ToString()));
	}
}