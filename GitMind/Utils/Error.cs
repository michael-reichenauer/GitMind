using System;


namespace GitMind.Utils
{
	public class Error : Equatable<Error>
	{
		private readonly string message;
		
		private Error(string message, Exception exception)
		{
			Exception = exception ?? new Exception();
			this.message = message;
		}
		
		public static Error None { get; } = From("No error");
		public static Error NoValue { get; } = From("No value");

		public Exception Exception { get; }

		public string Message => !string.IsNullOrEmpty(message) ?
			$"{message},\n{Exception.Message}" : Exception.Message;


		public static Error From(Exception e) => new Error(null, e);

		public static Error From(string message, Exception e) => new Error(message, e);

		public static Error From(string message) => new Error(null, new Exception(message));

	
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

		public override string ToString() => 
			$"{Exception.GetType().Name}, {Message}\n {Exception.StackTrace}";
	}
}