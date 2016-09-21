using System;


namespace GitMind.Utils
{
	public class Error : Equatable<Error>
	{
		private static readonly Exception noException = new Exception("none");
		private readonly Exception exception = null;

		private Error(string message = null)
			: this(null, message)
		{
		}

		private Error(Exception exception, string message = null)
		{
			if (message != null && exception != null)
			{
				Message = $"{message}, {exception.Message}";
				this.exception = exception;
			}
			else if (message == null && exception != null)
			{
				Message = exception.Message;
				this.exception = exception;
			}
			else
			{
				Message = "Error";
				this.exception = new Exception(Message);
			}

			if (exception != noException)
			{
				Log.Warn($"Error: {Message}");
			}
		}


		public string Message { get; }

		public static Error From(Exception e) => new Error(e);

		public static Error From(Exception e, string message) => new Error(e, message);

		public static Error From(string message) => new Error(message);

		public static Error None = new Error(noException);

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