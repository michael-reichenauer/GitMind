using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace GitMind.Git.Private
{
	internal class GitDiffParser : IGitDiffParser
	{
		private static readonly string DiffPart =
			"------------------------------------------------------"
			+ "------------------------------------------------";

		private static readonly string FilePart =
			"====================================================="
			+ "=================================================";


		public Task<CommitDiff> ParseAsync(string commitId, IReadOnlyList<string> diff)
		{
			return Task.Run(() =>
			{
				StringBuilder left = new StringBuilder();
				StringBuilder right = new StringBuilder();

				// MUst be fixed ####
				int diffFileIndex = 0;

				int index = 0;

				while (index != -1)
				{
					diffFileIndex++;
					string prefix = $"{diffFileIndex}| ";

					index = TryFindNextFile(index, diff);
					if (index == -1)
					{
						break;
					}

					left.AppendLine(prefix + FilePart);
					right.AppendLine(prefix + FilePart);

					string fileName = GetFileName(index, diff);
					left.AppendLine(prefix + fileName);
					right.AppendLine(prefix + fileName);

					left.AppendLine(prefix + FilePart);
					right.AppendLine(prefix + FilePart);

					index = FindFileDiff(index, diff);
					index = WriteFileDiff(index, diff, left, right, prefix);

					left.AppendLine("\n");
					right.AppendLine("\n");
				}

				string tempPath = Path.Combine(Path.GetTempPath(), "GitMind");
				if (!Directory.Exists(tempPath))
				{
					Directory.CreateDirectory(tempPath);
				}

				string shortId = commitId?.Substring(0, 6) ?? "";
				string leftName = $"Commit {shortId}-before";
				string rightName = $"Commit {shortId}-after";
				string leftPath = Path.Combine(tempPath, leftName);
				string rightPath = Path.Combine(tempPath, rightName);
				File.WriteAllText(leftPath, left.ToString());
				File.WriteAllText(rightPath, right.ToString());

				return new CommitDiff(leftPath, rightPath);
			});
		}


		private int TryFindNextFile(int index, IReadOnlyList<string> diff)
		{
			for (int i = index; i < diff.Count; i++)
			{
				string line = diff[i];
				if (line.StartsWith("--- "))
				{
					return i;
				}
			}

			return -1;
		}


		private int FindFileDiff(int index, IReadOnlyList<string> diff)
		{
			for (int i = index; i < diff.Count; i++)
			{
				string line = diff[i];
				if (!line.StartsWith("--- ") && !line.StartsWith("+++ "))
				{
					return i;
				}
			}

			return -1;
		}


		private string GetFileName(int index, IReadOnlyList<string> diff)
		{
			string line = diff[index];

			string sourceFileName = line.Substring(6);
			string targetFileName = sourceFileName;
			if (diff.Count > index + 1)
			{
				line = diff[index + 1];
				targetFileName = line.Substring(6);
			}

			if (sourceFileName != targetFileName)
			{
				if (sourceFileName == "ev/null")
				{
					return "Added: " + targetFileName;
				}
				else if (targetFileName == "ev/null")
				{
					return "Deleted: " + sourceFileName;
				}
				else
				{
					return "Renamed: " + sourceFileName + " -> " + targetFileName;
				}
			}
			else
			{
				return "Modified: " + sourceFileName;
			}
		}


		private static int WriteFileDiff(
			int index, IReadOnlyList<string> diff, StringBuilder left, StringBuilder right, string prefix)
		{
			if (index == -1)
			{
				return -1;
			}

			for (int i = index; i < diff.Count; i++)
			{
				string line = diff[i];
				if (line.StartsWith("@@ "))
				{
					RowInfo leftRowInfo = ParseLeftRow(line);
					RowInfo rightRowInfo = ParseRightRow(line);

					if (i != index)
					{
						left.AppendLine(prefix + DiffPart);
						right.AppendLine(prefix + DiffPart);
					}

					string rowNumbersLeft = $"{prefix}Lines {leftRowInfo.FirstRow}-{leftRowInfo.LastRow}:";
					string rowNumbersRight = $"{prefix}Lines {rightRowInfo.FirstRow}-{rightRowInfo.LastRow}:";
					left.AppendLine(rowNumbersLeft);
					left.AppendLine(prefix + DiffPart);
					right.AppendLine(rowNumbersRight);
					right.AppendLine(prefix + DiffPart);
				}
				else if (line.StartsWith(" "))
				{
					string text = GetTextLine(line);

					left.AppendLine(prefix + text);
					right.AppendLine(prefix + text);
				}
				else if (line.StartsWith("-"))
				{
					left.AppendLine(prefix + GetTextLine(line));
				}
				else if (line.StartsWith("+"))
				{
					right.AppendLine(prefix + GetTextLine(line));
				}
				else if (line.StartsWith(@"\ "))
				{
					// Ignore "\\ No new line rows"
					continue;
				}
				else
				{
					return i;
				}
			}

			return -1;
		}


		private static RowInfo ParseLeftRow(string line)
		{
			return ParseRow(line, "-");
		}


		private static RowInfo ParseRightRow(string line)
		{
			return ParseRow(line, "+");
		}


		private static RowInfo ParseRow(string line, string prefix)
		{
			int endIndex = line.IndexOf("@@", 2);
			if (endIndex > -1)
			{
				int startIndex = line.IndexOf(prefix);
				if (startIndex > -1 && startIndex < endIndex)
				{
					int index = line.IndexOf(",", startIndex);
					int stopIndex = line.IndexOf(" ", startIndex);

					index = Math.Min(index, stopIndex);

					if (index > -1 && index < endIndex)
					{
						string part = line.Substring(startIndex + 1, index - (startIndex + 1));

						int row = int.Parse(part);

						int count = 0;

						if (stopIndex > -1 && stopIndex > index)
						{
							string countPart = line.Substring(index + 1, stopIndex - (index + 1));
							count = int.Parse(countPart);
						}

						return new RowInfo(row, count);
					}
				}
			}

			return new RowInfo(1, 0);
		}


		private static string GetTextLine(string line)
		{
			// char[] b = line.ToCharArray();
			string textLine = line.Substring(1);
			if (textLine.StartsWith("\x00ef\x00bb\x00bf"))
			{
				// Line started with "magic" file header chars, removing them to make output nicer
				textLine = textLine.Substring(3);
			}

			return textLine;
		}


		private class RowInfo
		{
			public RowInfo(int row, int count)
			{
				FirstRow = row >= 0 ? row : 0;
				LastRow = count > 0 ? FirstRow + count - 1 : FirstRow;
			}

			public int FirstRow { get; }
			public int LastRow { get; }
		}
	}
}