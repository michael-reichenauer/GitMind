using System.Collections.Generic;
using System.Linq;


namespace GitMind.Utils.Git
{
	public class GitSetting
	{
		public GitSetting(string name, IReadOnlyList<string> values)
		{
			Name = name;
			Values = values;
		}


		public string Name { get; }
		public IReadOnlyList<string> Values { get; }

		public string Value => Values[0];

		public static implicit operator string(GitSetting setting) => setting.Value;

		public override string ToString() => Values.Count == 1 ? $"{Name}={Value}" :
			$"{Name} ({Values.Count}):\n" + string.Join("\n", Values.Select(v => $"    {Name}={v}"));
	}
}