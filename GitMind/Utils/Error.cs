namespace GitMind.Utils
{
	public class Error
	{
		public Error(string message = "")
		{
			Message = message ?? "";
		}


		public static Error From(string message = "") => new Error(message);

		public static Error None = new Error();
		public string Message { get; }
		public override string ToString() => Message;
	}
}