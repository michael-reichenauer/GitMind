using System;
using System.CodeDom;


namespace GitMind.Utils
{
	public class Error : Equatable<Error>
	{
		private readonly Exception exception = null;

		private Error(string message = null)
			: this(null, message)
		{
		}

		private Error(Exception exception, string message = null)
		{
			this.exception = exception;
			Message = message ?? exception?.Message ?? "";
		}

		public string Message { get; }

		public static Error From(Exception e) => new Error(e);

		public static Error From(string message = "") => new Error(message);

		public static Error None = new Error();


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