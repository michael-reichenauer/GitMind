using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace GitMind.Utils
{
	public class R
	{
		protected static readonly Exception NoError = new Exception("No error");
		protected static readonly Exception NoValueError = new Exception("No value");

		public static R Ok = new Error(NoError, null);
		public static Error NoValue = new Error(NoValueError, null);


		protected R(Exception e)
		{
			Exception = e;
		}

		public Exception Exception { get; }

		public bool IsOk => Exception == NoError;
		public bool IsFaulted => Exception != NoError;
		public string Message => Exception.Message;
		public string AllMessages => string.Join(",\n", AllMessageLines());

		public IEnumerable<string> AllMessageLines()
		{
			yield return Message;

			Exception inner = Exception.InnerException;
			while (inner != null)
			{
				yield return inner.Message;
				inner = inner.InnerException;
			}
		}


		public static R<T> From<T>(T result) => R<T>.From(result);

		public static Error Error(
			string message,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0) =>
			new Error(message, memberName, sourceFilePath, sourceLineNumber);


		public static Error Error(
			string message,
			Exception e,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0) =>
			new Error(message, e, memberName, sourceFilePath, sourceLineNumber);


		public static Error Error(
			Exception e,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0) =>
			new Error(e, memberName, sourceFilePath, sourceLineNumber);

		//public static implicit operator R(Exception e) => new RError(e);
		public static implicit operator bool(R r) => r.IsOk;

		public override string ToString() => IsOk ? "OK" : $"Error: {AllMessages}\n{Exception}";
	}


	public class R<T> : R
	{
		private readonly T storedValue;

		public new static readonly R<T> NoValue = new R<T>(NoValueError);

		private R(T value) : base(NoError) => this.storedValue = value;

		private R(Exception error) : base(error) { }


		//public static implicit operator R<T>(Error error) => new R<T>(error);
		//public static implicit operator R<T>(Exception e) => new R<T>(e);

		public static implicit operator R<T>(Error error) => new R<T>(error.Exception);
		public static implicit operator bool(R<T> r) => r.IsOk;

		public static implicit operator R<T>(T value)
		{
			if (value == null)
			{
				throw Asserter.FailFast("Value cannot be null");
			}

			return new R<T>(value);
		}

		public static R<T> From(T result) => new R<T>(result);

		public T Value => IsOk ? storedValue : throw Asserter.FailFast(Exception.ToString());


		public bool HasValue(out T value)
		{
			if (IsOk)
			{
				value = storedValue;
				return true;
			}
			else
			{
				value = default(T);
				return false;
			}
		}

		public T Or(T defaultValue) => IsFaulted ? defaultValue : Value;


		public override string ToString() => IsOk ? (storedValue?.ToString() ?? "") : base.ToString();
	}
}