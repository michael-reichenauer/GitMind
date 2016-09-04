using System;
using System.CodeDom;
using System.Diagnostics;


namespace GitMind.Utils
{
	public class Error : Equatable<Error>
	{
		private static readonly string none = "none";
		private readonly Exception exception = null;

		private Error(string message = null)
			: this(null, message)
		{
		}

		private Error(Exception exception, string message = null)
		{
			Message = message ?? "";
			this.exception = exception;

			if (message != null && exception != null)
			{
				Message = $"{message}, {exception.Message}";
			}
			else if (exception != null)
			{
				Message = exception.Message;
			}

			if (message != none)
			{
				Log.Warn($"Error: {Message}");
			}
		}


		public string Message { get; }

		public static Error From(Exception e) => new Error(e);

		public static Error From(Exception e, string message) => new Error(e, message);

		public static Error From(string message = "") => new Error(message);

		public static Error None = new Error(none);


		public bool Is<T>()
		{
			return this is T || exception?.GetType() is T;
		}


		protected override bool IsEqual(Error other)
		{
			return 
				(exception == null && other.exception == null && GetType() == other.GetType())
				|| other.GetType().IsInstanceOfType(this)
				|| (GetType() == other.GetType() && exception != null && other.exception != null 
					&& other.exception.GetType().IsInstanceOfType(this));
		}

		protected override int GetHash() => 0;

		public override string ToString() => Message;


		//public Error With(string message)
		//{
		//	return new Error(errorCode, Message + " " + message);
		//}

		//public Error With(Error error)
		//{
		//	return new Error(errorCode, Message + " " + error);
		//}

		//public Error With(R result)
		//{
		//	return new Error(errorCode, Message + " " + result);
		//}	
	}
}