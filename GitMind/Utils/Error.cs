using System;


namespace GitMind.Utils
{
	public class Error : Equatable<Error>
	{
		private static readonly Exception noError = new Exception("No error");
		private static readonly Exception noValue = new Exception("No value");

		private readonly string message;

		private Error(string message, Exception exception)
		{
			Exception = exception ?? new Exception();
			this.message = message ?? "";

			if (!((exception == noError || exception == noValue) && message == null))
			{
				Log.Warn(ToString());
			}
		}

		public static Error None { get; } = From(noError);
		public static Error NoValue { get; } = From(noValue);

		public Exception Exception { get; }

		public string Message => !string.IsNullOrEmpty(message) ? $"{message}" : Exception.Message;

		public string StackTrace => Exception.StackTrace != null ? $"at:\n{Exception.StackTrace}" : null;

		public static Error From(string message, Error error) => new Error(message, error.Exception);
		public static Error From(string message, R result) => From(message, result.Error);

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


		public override string ToString() => !string.IsNullOrEmpty(message)
			? $"{Message}\n{Exception.GetType().Name}, {Exception.Message}{StackTrace}"
			: $"{Exception.GetType().Name}, {Exception.Message}{StackTrace}";
	}
}