namespace System.Text.RegularExpressions
{
	internal static class RegexExtensions
	{
		public static bool TryMatch(this Regex regex, string text, out Match match)
		{
			match = regex.Match(text);
			return match.Success;
		}
	}
}