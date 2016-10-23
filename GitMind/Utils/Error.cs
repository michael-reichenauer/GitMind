using System;


namespace GitMind.Utils
{
	public class Error : Equatable<Error>
	{
		private static readonly Exception errorException = new Exception("Error");
		private static readonly Exception noErrorException = new Exception("No error");
		private static readonly Exception noValueException = new Exception("No value");

		private readonly Exception exception = null;


		public static Error None = new Error(noErrorException);

		public static Error NoValue = new Error(noValueException);


		private Error(string message = null)
			: this(null, message)
		{
		}

		private Error(Exception exception, string message = null)
		{
			exception = exception ?? errorException;

			if (message != null && exception != errorException)
			{
				Message = $"{message},\n{exception.GetType().Name}: {exception.Message}";
				this.exception = exception;
			}
			else if (message != null)
			{
				Message = $"{message}";
				this.exception = exception;
			}
			else 
			{
				Message = exception.Message;
				this.exception = exception;
			}

			if (exception != noErrorException && exception != noValueException)
			{
				Log.Warn($"Error: {Message}");
			}
		}


		public string Message { get; }

		public Exception Exception => exception;

		public static Error From(Exception e) => new Error(e);

		public static Error From(Exception e, string message) => new Error(e, message);

		public static Error From(string message) => new Error(message);

	
		public static implicit operator Error(Exception e) => new Error(e);


		public bool Is<T>()
		{
			return this is T || exception is T;
		}


		protected override bool IsEqual(Error other)
		{
			if ((ReferenceEquals(this, None) && !ReferenceEquals(other, None))
					|| !ReferenceEquals(this, None) && ReferenceEquals(other, None))
			{
				return false;
			}

			return
				(exception == null && other.exception == null && GetType() == other.GetType())
				|| other.GetType().IsInstanceOfType(this)
				|| (GetType() == other.GetType() && exception != null && other.exception != null
					&& other.exception.GetType().IsInstanceOfType(this));
		}

		protected override int GetHash() => 0;

		public override string ToString() => Message;
	}
}