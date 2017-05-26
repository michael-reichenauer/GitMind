using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	[SingleInstance]
	internal class LinkService : ILinkService
	{
		private static readonly string splitChars = @"[\,; :]";

		private readonly WorkingFolder workingFolder;
		private readonly IMessageService messageService;

		private List<Pattern> issuePatterns;
		private List<Pattern> tagPatterns;
		private Regex issuesRgx;
		private Regex tagsRgx;
		private bool initialized = false;


		public LinkService(
			WorkingFolder workingFolder,
			IMessageService messageService)
		{
			this.workingFolder = workingFolder;
			this.messageService = messageService;

			workingFolder.OnChange += (s, e) => initialized = false;
		}


		public Links ParseIssues(string text)
		{
			if (!initialized)
			{
				ParsePatternsForWorkingFolder();
			}

			return Parse(text, issuesRgx, issuePatterns);
		}


		public Links ParseTags(string text)
		{
			if (!initialized)
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
			initialized = true;
		}


		private void ParseFile()
		{
			string filePath = Path.Combine(workingFolder, ".gitmind");
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
				Log.Warn($"Failed to parse {filePath} {e}");
				messageService.ShowError($"Failed to parse {filePath}\n{e.Message}");
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to parse {filePath} {e}");
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
						string matchValue = groupMatch.Groups[1].Value;

						string uri = string.Format(patterns[i].LinkPattern, matchValue);

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