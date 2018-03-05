using System.Collections.Generic;
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


		public async Task<IReadOnlyDictionary<string, string>> GetAsync(CancellationToken ct)
		{
			CmdResult2 result = await gitCmd.RunAsync(ConfigListArgs, ct);
			result.ThrowIfError("Failed to get config list");

			Dictionary<string, string> settings = new Dictionary<string, string>();

			foreach (string line in result.OutputLines)
			{
				string settingText = line.Trim();
				if (!string.IsNullOrEmpty(settingText))
				{
					string[] setting = settingText.Split(EqualChar);
					settings[setting[0]] = setting[1];
				}
			}

			return settings;
		}
	}
}