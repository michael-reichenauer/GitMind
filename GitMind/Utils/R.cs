using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace GitMind.Utils
{
	public class R
	{
		protected static readonly Exception NoError = new Exception("No error");
		protected static readonly Exception NoValueError = new Exception("No value");

		public static R Ok = new RError(NoError, null);
		public static RError NoValue = new RError(NoValueError, null);


		protected R(Exception e)
		{
			Exception = e;		
		}

		public Exception Exception { get; }

		public bool IsOk => Exception == NoError;
		public bool IsFaulted => !Ok;
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

		public static RError Error(
			string message,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0) =>
			new RError(new Exception(message), ToStackTrace(memberName, sourceFilePath, sourceLineNumber));

		public static RError Error(
			string message,
			Exception e,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0) =>
			new RError(new Exception(message, e), ToStackTrace(memberName, sourceFilePath, sourceLineNumber));



		public static RError Error(
			Exception e,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0) => 
			new RError(e, ToStackTrace(memberName, sourceFilePath, sourceLineNumber));

		//public static implicit operator R(Exception e) => new RError(e);
		public static implicit operator bool(R r) => r.IsOk;

		public override string ToString() => IsOk ? "OK" : $"Error: {AllMessages}\n{Exception}";

		private static string ToStackTrace(string memberName, string sourceFilePath, int sourceLineNumber) =>
			$"at {sourceFilePath}({sourceLineNumber}){memberName}";
	}


	public class RError : R
	{
		public RError(Exception e, string stackTrace) : base(AddStackTrace(e, stackTrace))
		{
			if (e != NoError && e != NoValueError)
			{
				Log.Warn($"{this}");
			}
		}

		private static Exception AddStackTrace(Exception exception, string stackTrace)
		{
			if (stackTrace == null)
			{
				return exception;
			}

			FieldInfo field = typeof(Exception).GetField(
				"_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);

			string stack = (string)field?.GetValue(exception);
			stackTrace = string.IsNullOrEmpty(stack) ? stackTrace : $"{stackTrace}\n{stack}";
			field?.SetValue(exception, stackTrace);
			return exception;
		}
	}


	public class R<T> : R
	{
		private readonly T storedValue;

		public new static readonly R<T> NoValue = new R<T>(NoValueError);

		private R(T value) : base(NoError) => this.storedValue = value;

		private R(Exception error) : base(error) { }


		//public static implicit operator R<T>(Error error) => new R<T>(error);
		//public static implicit operator R<T>(Exception e) => new R<T>(e);

		public static implicit operator R<T>(RError error) => new R<T>(error.Exception);
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


		public override string ToString() =>IsOk ? (storedValue?.ToString() ?? "") : base.ToString();
	}
}