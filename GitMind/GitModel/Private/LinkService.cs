using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GitMind.Common.MessageDialogs;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	[SingleInstance]
	internal class LinkService : ILinkService
	{
		private static readonly string splitChars = @"[\,; :]";
		private readonly IMessageService messageService;
        private readonly IGitSettings gitSettings;

        private List<Pattern> issuePatterns;
		private List<Pattern> tagPatterns;
		private Regex issuesRgx;
		private Regex tagsRgx;
        private string parsedWorkingFolder = "";


		public LinkService(
			IMessageService messageService,
            IGitSettings gitSettings)
		{
			this.messageService = messageService;
            this.gitSettings = gitSettings;
		}


		public Links ParseIssues(string text)
		{
			if (parsedWorkingFolder != gitSettings.WorkingFolderPath)
			{
				ParsePatternsForWorkingFolder();
			}

			return Parse(text, issuesRgx, issuePatterns);
		}


		public Links ParseTags(string text)
		{
            if (parsedWorkingFolder != gitSettings.WorkingFolderPath)
            {
                ParsePatternsForWorkingFolder();
            }
            return Parse(text, tagsRgx, tagPatterns);
		}


		private void ParsePatternsForWorkingFolder()
		{
			issuePatterns = new List<Pattern>();
			tagPatterns = new List<Pattern>();

			ParseFile();

			issuesRgx = GetPatternsRegExp(issuePatterns);
			tagsRgx = GetPatternsRegExp(tagPatterns);
            parsedWorkingFolder =gitSettings.WorkingFolderPath;
		}


		private void ParseFile()
		{
			string filePath = Path.Combine(gitSettings.WorkingFolderPath, ".gitmind");
			try
			{
				if (!File.Exists(filePath))
				{
					issuePatterns.Add(new Pattern(
						"https://github.com/michael-reichenauer/GitMind/wiki/Help#links",
						@"#(\d+)", 
						LinkType.issue));
					tagPatterns.Add(new Pattern(
						"https://github.com/michael-reichenauer/GitMind/wiki/Help#links",
						@"(.+)",
						LinkType.tag));
					return;
				}

				string[] fileLines = File.ReadAllLines(filePath);
				foreach (string line in fileLines)
				{
					if (line.StartsWithOic("issue:"))
					{
						int patternIndex = line.IndexOfOic(" ", 8);
						string linkPattern = line.Substring(6, patternIndex - 6).Trim();
						string regexp = line.Substring(patternIndex).Trim();
						issuePatterns.Add(new Pattern(linkPattern, regexp, LinkType.issue));
					}
					else if (line.StartsWithOic("tag:"))
					{
						int patternIndex = line.IndexOfOic(" ", 6);
						string linkPattern = line.Substring(4, patternIndex - 4).Trim();
						string regexp = line.Substring(patternIndex).Trim();

						tagPatterns.Add(new Pattern(linkPattern, regexp, LinkType.tag));
					}
				}
			}
			catch (ArgumentException e)
			{
				Log.Exception(e, $"Failed to parse {filePath}");
				messageService.ShowError($"Failed to parse {filePath}\n{e.Message}");
			}
			catch (Exception e)
			{
				Log.Exception(e, $"Failed to parse {filePath}");
				messageService.ShowError($"Failed to parse {filePath}\n{e.Message}");
			}			
		}


		private static Regex GetPatternsRegExp(IEnumerable<Pattern> patterns)
		{
			string issueTxt = "";
			int index = 0;
			foreach (Pattern pattern in patterns)
			{
				if (index > 0)
				{
					issueTxt += "|";
				}

				issueTxt += $"(?<n{index}>{splitChars}*{pattern.RegExp}{splitChars}*)";
				index++;
			}

			return new Regex(issueTxt);
		}



		private static Links Parse(string text, Regex rgx, IReadOnlyList<Pattern> patterns)
		{
			string totalText = "";
			List<Link> links = new List<Link>();
			foreach (Match match in rgx.Matches(text ?? ""))
			{
				totalText += match.Value;

				for (int i = 0; i < patterns.Count; i++)
				{
					string patternMatch = match.Groups["n" + i].Value;

					if (!string.IsNullOrEmpty(patternMatch))
					{
						var groupMatch = patterns[i].Rgx.Match(patternMatch);

						string matchText = groupMatch.Groups[0].Value;
						var matchValues = groupMatch.Groups.Cast<Group>().Skip(1).Cast<object>().ToArray();

						string uri = string.Format(patterns[i].LinkPattern, matchValues);

						Link link = new Link(matchText, uri, patterns[i].LinkType);
						links.Add(link);
						break;
					}
				}			
			}

			return new Links(links, totalText);
		}
	}
}