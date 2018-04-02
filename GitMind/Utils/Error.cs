using System;


namespace GitMind.Utils
{
	public class Error : Equatable<Error>
	{
		private Error(Exception e, string message, string text)
		{
			Exception = e;
			Message = message;
			Text = text;
		}
		
		public static Error None { get; } = new Error(new Exception("No error"), "No error", "No error");
		public static Error NoValue { get; } = new Error(new Exception("No value"), "No value", "No value");

		public string Message { get; }
		public string Text { get; }
		public Exception Exception { get; }

		public static Error From(Exception e) => 
			new Error(e, e.Message, $"{e.GetType().Name}: {e.Message}");

		public static Error From(Exception e, string message) =>
			new Error(e, $"{message}; {e.Message}", $"{message},\n{e.GetType().Name}: {e.Message}");

		public static Error From(string message) =>
			new Error(new Exception(message), message, $"{message},\nException: {message}");

	
		public static implicit operator Error(Exception e) => From(e);


		public bool Is<T>()
		{
			return this is T || Exception is T;
		}


		protected override bool IsEqual(Error other)
		{
			if ((ReferenceEquals(this, None) && !ReferenceEquals(other, None))
					|| !ReferenceEquals(this, None) && ReferenceEquals(other, None))
			{
				return false;
			}

			return
				(Exception == null && other.Exception == null && GetType() == other.GetType())
				|| other.GetType().IsInstanceOfType(this)
				|| (GetType() == other.GetType() && Exception != null && other.Exception != null
					&& other.Exception.GetType().IsInstanceOfType(this));
		}

		protected override int GetHash() => 0;

		public override string ToString() => Text;
	}
}