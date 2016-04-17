using System;


namespace GitMind.Utils
{
	public class Error : IEquatable<Error>
	{
		private readonly Guid errorCode;


		public Error(string message = "")
			: this(Guid.NewGuid(), message)
		{
		}

		private Error(Guid errorCode, string message)
		{
			this.errorCode = errorCode;
			Message = message ?? "";
		}

		public static Error From(Exception e) => new Error(e.Message);
		public static Error From(string message = "") => new Error(message);

		public static Error None = new Error();
		public string Message { get; }
		public override string ToString() => Message;


		public Error With(string message)
		{
			return new Error(errorCode, Message + " " + message);
		}

		public Error With(Error error)
		{
			return new Error(errorCode, Message + " " + error);
		}

		public Error With(Result result)
		{
			return new Error(errorCode, Message + " " + result);
		}


		public bool Equals(Error other)
		{
			return (other != null) && (errorCode == other.errorCode);
		}

		public override bool Equals(object obj)
		{
			return obj is Error && Equals((Error)obj);
		}

		public static bool operator ==(Error obj1, Error obj2)
		{
			if (ReferenceEquals(obj1, null) && ReferenceEquals(obj2, null))
			{
				return true;
			}
			else if (ReferenceEquals(obj1, null) || ReferenceEquals(obj2, null))
			{
				return false;
			}
			else
			{
				return obj1.Equals(obj2);
			}
		}

		public static bool operator !=(Error obj1, Error obj2)
		{
			return !(obj1 == obj2);
		}

		public override int GetHashCode()
		{
			return errorCode.GetHashCode();
		}
	}
}