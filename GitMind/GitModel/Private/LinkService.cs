using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
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
		private bool initialised = false;


		public LinkService(
			WorkingFolder workingFolder,
			IMessageService messageService)
		{
			this.workingFolder = workingFolder;
			this.messageService = messageService;

			workingFolder.OnChange += (s, e) => initialised = false;
		}


		public Links ParseIssues(string text)
		{
			if (!initialised)
			{
				OnChangedWorkingFolder();
			}

			return Parse(text, issuesRgx, issuePatterns);
		}


		public Links ParseTags(string text)
		{
			if (!initialised)
			{
				OnChangedWorkingFolder();
			}

			return Parse(text, tagsRgx, tagPatterns);
		}


		private void OnChangedWorkingFolder()
		{
			issuePatterns = new List<Pattern>();
			tagPatterns = new List<Pattern>();

			ParseFile();

			issuesRgx = GetPatternsRegExp(issuePatterns);
			tagsRgx = GetPatternsRegExp(tagPatterns);
			initialised = true;
		}


		private void ParseFile()
		{
			string filePath = Path.Combine(workingFolder, ".gitmind");
			try
			{
				string[] fileLines = File.ReadAllLines(filePath);
				foreach (string line in fileLines)
				{
					if (line.StartsWith("issue:", StringComparison.OrdinalIgnoreCase))
					{
						int patternIndex = line.IndexOf(" ", 8);
						string linkPattern = line.Substring(6, patternIndex - 6).Trim();
						string regexp = line.Substring(patternIndex).Trim();
						issuePatterns.Add(new Pattern(linkPattern, regexp, LinkType.issue));
					}
					else if (line.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
					{
						int patternIndex = line.IndexOf(" ", 6);
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



		private Links Parse(string text, Regex rgx, List<Pattern> patterns)
		{
			string totalText = "";
			List<Link> links = new List<Link>();
			foreach (Match match in rgx.Matches(text ?? ""))
			{
				totalText += match.Value;

				for (int i = 0; i < patterns.Count; i++)
				{
					string g1 = match.Groups["n" + i].Value;
					if (!string.IsNullOrEmpty(g1))
					{
						var m1 = patterns[i].Rgx.Match(g1);
						string matchText = m1.Groups[0].Value;
						string value = m1.Groups[1].Value;
						string uri = string.Format(patterns[i].LinkPattern, value);

						Link item = new Link(matchText, uri, patterns[i].LinkType);
						links.Add(item);
						break;
					}
				}			
			}

			return new Links(links, totalText);
		}
	}
}