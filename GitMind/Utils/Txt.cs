namespace System
{
	internal static class Txt
	{
		public static int CompareOic(string strA, string strB) => 
			string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase);

		public static int IndexOfOic(this string text, string value) =>
			text.IndexOf(value, StringComparison.OrdinalIgnoreCase);

		public static int IndexOfOic(this string text, string value, int index) =>
			text.IndexOf(value, index, StringComparison.OrdinalIgnoreCase);

		public static bool StartsWithOic(this string text, string value) => 
			text.StartsWith(value, StringComparison.OrdinalIgnoreCase);
	}
}